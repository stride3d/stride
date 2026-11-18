// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Stride.Assets;

/// <summary>
/// Describes types replaced by types of another library, for <see cref="CodeUpgrades.MigrateTypes"/>. Type and
/// parameter type names are full metadata names of the old types.
/// </summary>
/// <remarks>
/// Written for the move of the math types to System.Numerics (<see cref="CodeUpgrades.SystemNumericsMigration"/>), its
/// only user: it has the rules that move needed.
/// </remarks>
public sealed class TypeMigration
{
    internal readonly Dictionary<string, string> Types = [];
    internal readonly HashSet<string> NewFormTypes = [];
    internal readonly List<(string Type, string Member, string NewMember, string[] ParameterTypes)> Renames = [];
    internal readonly List<(string Type, string Member, string[] ParameterTypes, string ResultType, string NewType, string NewMember)> Moves = [];
    internal readonly HashSet<(string Type, string Method)> Helpers = [];
    internal readonly Dictionary<string, int> LinearIndexers = [];
    internal readonly Dictionary<(string Type, string Property), string> Setters = [];
    internal readonly List<(string Type, string Operator, string Method)> Operators = [];
    internal readonly List<(string Type, string Operator, string Method, bool Negate)> InstanceOperators = [];
    internal readonly List<(string Type, string[] ParameterTypes, string Method)> Constructors = [];
    internal readonly List<(string Type, string[] ParameterTypes, string Expression)> ConstructorExpressions = [];
    internal readonly List<(string From, string To, string Method)> Casts = [];
    internal readonly HashSet<(string From, string To)> ImplicitCasts = [];
    internal readonly HashSet<(string Type, string From)> NewConversions = [];
    internal readonly List<(string Type, string Member, string Method, string Expression, string ResultName)> OutCalls = [];
    internal bool TwinsOnly;
    internal readonly List<(string Type, string Member, string Expression, string Import)> MemberExpressions = [];
    internal readonly List<(string Type, string Method, string[] ParameterTypes, string Expression, string[] FirstArguments, string Import)> CallExpressions = [];
    internal readonly Dictionary<(string Type, string Method), string> RefCalls = [];
    internal readonly Dictionary<(string Type, string Method), string> InPlaceExpressions = [];
    internal readonly List<(string Type, string Method, string[] ParameterTypes, string Expression)> Deconstructions = [];
    internal readonly Dictionary<(string Type, string Operator), string> OperatorExpressions = [];
    internal readonly Dictionary<(string Type, string Property), (string Expression, string Import)> SetterExpressions = [];
    internal readonly Dictionary<string, (string[] Members, string Read)> Indexers = [];
    internal readonly Dictionary<string, string> FormatMethods = [];

    /// <summary>Replaces <paramref name="oldType"/> by <paramref name="newType"/>, a type with another full name.</summary>
    public TypeMigration Type(string oldType, string newType)
    {
        Types.Add(oldType, newType);
        return this;
    }

    /// <summary>
    /// Names a type that stays, with members that change: the rules for the members of a replaced type apply to it, and
    /// its name is left alone.
    /// </summary>
    public TypeMigration KeepType(string type)
    {
        Types.Add(type, type);
        return this;
    }

    /// <summary>
    /// Says that a type that stays now has its functions in the forms of the new library: one form per operation, which
    /// takes values and returns its result. The rules for the forms of calls then apply to it as to a replaced type.
    /// </summary>
    public TypeMigration NewForms(string type)
    {
        NewFormTypes.Add(type);
        return this;
    }

    /// <summary>
    /// Renames a member of a replaced type. With <paramref name="parameterTypes"/>, only the method overload that takes these
    /// values (also the one that takes them by reference and gives its result through a last <c>out</c> parameter).
    /// </summary>
    public TypeMigration Rename(string type, string member, string newMember, params string[] parameterTypes)
    {
        Renames.Add((type, member, newMember, parameterTypes.Length > 0 ? parameterTypes : null));
        return this;
    }

    /// <summary>
    /// Moves the static method overload that takes these values and gives this result to another replaced type, under the
    /// same name or another: <c>A.M(x)</c> → <c>B.N(x)</c>.
    /// </summary>
    /// <param name="newType">The old name of the replaced type that now has the method.</param>
    public TypeMigration Move(string type, string member, string[] parameterTypes, string resultType, string newType, string newMember = null)
    {
        Moves.Add((type, member, parameterTypes, resultType, newType, newMember ?? member));
        return this;
    }

    /// <summary>
    /// Replaces an indexer that numbers the elements of a matrix row after row by the indexer of the new type, which takes a
    /// row and a column: <c>m[i]</c> → <c>m[i / columns, i % columns]</c>.
    /// </summary>
    public TypeMigration LinearIndexer(string type, int columns)
    {
        LinearIndexers.Add(type, columns);
        return this;
    }

    /// <summary>Replaces the assignment of a property by a call of a method: <c>x.P = value;</c> → <c>x.Method(value);</c>.</summary>
    public TypeMigration SetterToCall(string type, string property, string method)
    {
        Setters.Add((type, property), method);
        return this;
    }

    /// <summary>
    /// Declares a static method that the new library adds to the new type from outside, as an extension member: the old
    /// library is all the upgrade sees, so it cannot find that method by itself.
    /// </summary>
    /// <param name="method">The name of the method in the new library.</param>
    public TypeMigration Helper(string type, string method)
    {
        Helpers.Add((type, method));
        return this;
    }

    /// <summary>
    /// Replaces a binary operator between two values of a replaced type by a call of a static method of the new type:
    /// <c>a * b</c> → <c>Type.Method(a, b)</c>, and <c>a *= b</c> → <c>a = Type.Method(a, b)</c>.
    /// </summary>
    /// <param name="operatorName">The metadata name of the operator, such as <c>op_Multiply</c>.</param>
    public TypeMigration OperatorToCall(string type, string operatorName, string method)
    {
        Operators.Add((type, operatorName, method));
        return this;
    }

    /// <summary>
    /// Replaces a binary operator between two values of a replaced type by a call of an instance or extension method on the
    /// left one: <c>a == b</c> → <c>a.Method(b)</c>, or <c>!a.Method(b)</c> with <paramref name="negate"/>.
    /// A comparison with a nullable value or with <c>null</c> stays as it is.
    /// </summary>
    /// <param name="operatorName">The metadata name of the operator, such as <c>op_Equality</c>.</param>
    public TypeMigration OperatorToInstanceCall(string type, string operatorName, string method, bool negate = false)
    {
        InstanceOperators.Add((type, operatorName, method, negate));
        return this;
    }

    /// <summary>Replaces a constructor of a replaced type by a call of a static method of the new type.</summary>
    public TypeMigration ConstructorToCall(string type, string[] parameterTypes, string method)
    {
        Constructors.Add((type, parameterTypes, method));
        return this;
    }

    /// <summary>
    /// Replaces a constructor of a replaced type by an expression of its arguments, where <c>{0}</c> is the first one:
    /// <c>new T(a, b)</c> → <c>new T(b, U.Method(b, a))</c> for <c>"new T({1}, U.Method({1}, {0}))"</c>.
    /// </summary>
    /// <remarks>
    /// The expression names the new types without their namespace. An argument that it uses twice and that may not read
    /// the same twice (a call) is computed once: <c>(a, F()) is var (x, y) ? new T(y, U.Method(y, x)) : default</c>.
    /// </remarks>
    public TypeMigration ConstructorToExpression(string type, string[] parameterTypes, string expression)
    {
        ConstructorExpressions.Add((type, parameterTypes, expression));
        return this;
    }

    /// <summary>Replaces an explicit conversion between two replaced types by a call of an instance or extension method: <c>(To)x</c> → <c>x.Method()</c>.</summary>
    public TypeMigration CastToCall(string from, string to, string method)
    {
        Casts.Add((from, to, method));
        return this;
    }

    /// <summary>
    /// Says that the new type converts to <paramref name="to"/> only when asked, where the old one did by itself: the
    /// cast is written, <c>float f = x;</c> → <c>float f = (float)x;</c>.
    /// </summary>
    public TypeMigration ImplicitToCast(string from, string to)
    {
        ImplicitCasts.Add((from, to));
        return this;
    }

    /// <summary>
    /// Says that the new type converts by itself from <paramref name="from"/>, which the old one did not. A method that
    /// takes either the replaced type or another one would then be ambiguous for such an argument: the argument is given
    /// the type of the parameter it had, <c>new T(1, b)</c> → <c>new T(1f, (float)b)</c>.
    /// </summary>
    public TypeMigration ConvertsFrom(string type, params string[] from)
    {
        foreach (var source in from)
            NewConversions.Add((type, source));
        return this;
    }

    /// <summary>
    /// Replaces the methods of a name, which give a new value of their type, by a static method that gives it through a
    /// last <c>out</c> parameter: <c>T.M(ref a, out b);</c> → <c>T.Method(a, out b);</c>, <c>x.M();</c> →
    /// <c>T.Method(x, out x);</c>, <c>var b = T.M(a);</c> → <c>T.Method(a, out var b);</c> and <c>b = T.M(a);</c> →
    /// <c>T.Method(a, out b);</c>.
    /// </summary>
    /// <param name="expression">
    /// What a call becomes where no statement can take its place, inside a larger expression: <c>{0}</c> is its first
    /// argument and <c>{result}</c> a new variable, as in <c>"T.Other({0}, out var {result}) ? {result} : default"</c>.
    /// It names the new types without their namespace.
    /// </param>
    /// <param name="resultName">The name of that variable; a number follows it where the name is taken.</param>
    public TypeMigration ToOutCall(string type, string member, string method, string expression, string resultName)
    {
        OutCalls.Add((type, member, method, expression, resultName));
        return this;
    }

    /// <summary>
    /// Says that the new library has one form of each static method: where a replaced type had a method that gives its
    /// result through a last <c>out</c> parameter and a twin that returns it, only the twin is left, also among the
    /// extension members. <c>T.M(ref a, out b);</c> → <c>b = T.M(a);</c> for every such pair.
    /// </summary>
    public TypeMigration ReturningTwinsOnly()
    {
        TwinsOnly = true;
        return this;
    }

    /// <summary>
    /// Replaces a field or a property the new library does not have by an expression, where <c>{0}</c> is the value it is
    /// read on: <c>x.P</c> → <c>x.Method(T.Other)</c> for <c>"{0}.Method(T.Other)"</c>. For a static member, the
    /// expression has no value to name.
    /// </summary>
    /// <param name="import">A namespace the expression needs, besides the ones of the new and of the old type.</param>
    public TypeMigration MemberToExpression(string type, string member, string expression, string import = null)
    {
        MemberExpressions.Add((type, member, expression, import));
        return this;
    }

    /// <summary>
    /// Replaces the static method that takes these values by an expression of its arguments, where <c>{0}</c> is the
    /// first one: <c>T.M(a)</c> → <c>a.P</c> for <c>"{0}.P"</c>. Also the form of that method that gives its result
    /// through a last <c>out</c> parameter, which can be its only form.
    /// </summary>
    /// <param name="firstArguments">
    /// When given, the rule is only for the calls whose first argument is one of these static members of its type, or
    /// what one of them returns. Such a rule goes before the one for the other calls of the method.
    /// </param>
    /// <param name="import">A namespace the expression needs, besides the ones of the new and of the old type.</param>
    public TypeMigration CallToExpression(string type, string method, string[] parameterTypes, string expression, string[] firstArguments = null, string import = null)
    {
        CallExpressions.Add((type, method, parameterTypes, expression, firstArguments, import));
        return this;
    }

    /// <summary>
    /// Replaces a method that changes the variable it takes by <c>ref</c> by an assignment to that variable, where
    /// <c>{0}</c> is the value the method is called on and <c>{1}</c> the variable: <c>x.M(ref a);</c> →
    /// <c>a = T.N(a, x);</c> for <c>"T.N({1}, {0})"</c>.
    /// </summary>
    public TypeMigration RefCallToAssignment(string type, string method, string expression)
    {
        RefCalls.Add((type, method), expression);
        return this;
    }

    /// <summary>
    /// Replaces a method that changes the value it is called on by an assignment of an expression to that value, where
    /// <c>{0}</c> is the value and <c>{1}</c> the first argument: <c>x.M(a);</c> → <c>x = T.N(x, a);</c> for
    /// <c>"T.N({0}, {1})"</c>.
    /// </summary>
    public TypeMigration InPlaceToExpression(string type, string method, string expression)
    {
        InPlaceExpressions.Add((type, method), expression);
        return this;
    }

    /// <summary>
    /// Replaces the static method that takes these values and gives its results through the <c>out</c> parameters that
    /// follow them by the deconstruction of an expression of the values: <c>T.M(a, out b, out c);</c> →
    /// <c>(b, c) = a.P;</c> for <c>"{0}.P"</c>.
    /// </summary>
    public TypeMigration OutsToDeconstruction(string type, string method, string[] parameterTypes, string expression)
    {
        Deconstructions.Add((type, method, parameterTypes, expression));
        return this;
    }

    /// <summary>
    /// Replaces an operator on one value of a replaced type, or between two, by an expression of its operands:
    /// <c>+a</c> → <c>a</c> for <c>"{0}"</c>.
    /// </summary>
    /// <param name="operatorName">The metadata name of the operator, such as <c>op_UnaryPlus</c>.</param>
    public TypeMigration OperatorToExpression(string type, string operatorName, string expression)
    {
        OperatorExpressions.Add((type, operatorName), expression);
        return this;
    }

    /// <summary>
    /// Replaces the assignment of a property by the assignment of an expression to the value it is set on, where
    /// <c>{0}</c> is that value and <c>{1}</c> the assigned one: <c>x.P = a;</c> → <c>x = x with { F = a };</c> for
    /// <c>"{0} with { F = {1} }"</c>.
    /// </summary>
    /// <param name="import">A namespace the expression needs, besides the ones of the new and of the old type.</param>
    public TypeMigration SetterToAssignment(string type, string property, string expression, string import = null)
    {
        SetterExpressions.Add((type, property), (expression, import));
        return this;
    }

    /// <summary>
    /// Replaces an indexer by an integer that the new type does not have. With a constant index, the member that index
    /// names: <c>x[1]</c> → <c>x.B</c>. Otherwise a read becomes <paramref name="read"/>, where <c>{0}</c> is the value
    /// and <c>{1}</c> the index; a write stays as it is.
    /// </summary>
    public TypeMigration IndexerToMembers(string type, string[] members, string read)
    {
        Indexers.Add(type, (members, read));
        return this;
    }

    /// <summary>
    /// Says that the new type does not format itself, where the old one did: <c>$"{x:F2}"</c> →
    /// <c>$"{x.Method("F2", null)}"</c>.
    /// </summary>
    public TypeMigration FormatToCall(string type, string method)
    {
        FormatMethods.Add(type, method);
        return this;
    }
}

public static partial class CodeUpgrades
{
    /// <summary>
    /// Migrates code from the math types of <c>Stride.Core.Mathematics</c> that the types of System.Numerics replace:
    /// <c>Vector2</c>, <c>Vector3</c>, <c>Vector4</c>, <c>Quaternion</c>, <c>Matrix</c> and <c>Plane</c>.
    /// <c>Double2</c>, <c>Double3</c> and <c>Double4</c> stay, with the names and the forms of these vectors.
    /// So do <c>Int2</c>, <c>Int3</c>, <c>Int4</c>, <c>UInt4</c>, <c>Half2</c>, <c>Half3</c> and <c>Half4</c>.
    /// <c>Half</c> becomes <see cref="System.Half"/>.
    /// </summary>
    public static CodeUpgrade MathTypesToSystemNumerics() => MigrateTypes(SystemNumericsMigration());

    /// <summary>
    /// Describes <see cref="MathTypesToSystemNumerics"/>. A member takes the name of a System.Numerics member when both
    /// give the same results for the values Stride asked for; a comment says where they differ for others. One that
    /// behaves differently keeps its Stride name, which <c>Stride.Core.Mathematics</c> then has as an extension member,
    /// or goes to a helper with the Stride behavior.
    /// </summary>
    public static TypeMigration SystemNumericsMigration()
    {
        const string Namespace = "Stride.Core.Mathematics.";
        const string Vector2 = Namespace + "Vector2";
        const string Vector3 = Namespace + "Vector3";
        const string Vector4 = Namespace + "Vector4";
        const string Quaternion = Namespace + "Quaternion";
        const string Matrix = Namespace + "Matrix";
        const string Plane = Namespace + "Plane";
        string[] types = [Vector2, Vector3, Vector4, Quaternion, Matrix, Plane];

        var migration = new TypeMigration()
            .ReturningTwinsOnly()
            .Type(Vector2, "System.Numerics.Vector2")
            .Type(Vector3, "System.Numerics.Vector3")
            .Type(Vector4, "System.Numerics.Vector4")
            .Type(Quaternion, "System.Numerics.Quaternion")
            .Type(Matrix, "System.Numerics.Matrix4x4")
            .Type(Plane, "System.Numerics.Plane");

        foreach (var type in types)
        {
            // Stride compared with a tolerance; == and Equals of System.Numerics are exact
            migration
                .Rename(type, "Equals", "NearEquals", type)
                .Rename(type, "EqualsStrict", "Equals")
                .OperatorToInstanceCall(type, "op_Equality", "NearEquals")
                .OperatorToInstanceCall(type, "op_Inequality", "NearEquals", negate: true);
        }

        // What System.Numerics gives with one call of its own
        foreach (var (type, name) in ((string, string)[])[(Vector2, "Vector2"), (Vector3, "Vector3"), (Vector4, "Vector4"), (Quaternion, "Quaternion"), (Matrix, "Matrix4x4")])
            migration.MemberToExpression(type, "SizeInBytes", $"Unsafe.SizeOf<{name}>()", "System.Runtime.CompilerServices");

        foreach (var vector in (string[])[Vector2, Vector3, Vector4])
        {
            migration
                .Rename(vector, "Modulate", "Multiply")
                .Rename(vector, "Demodulate", "Divide")
                // Zero stays zero; Normalize of System.Numerics gives NaN
                .Rename(vector, "Normalize", "NormalizeSafe")
                .Helper(vector, "NormalizeSafe");
        }

        // Floor and Ceiling are Round towards one side in System.Numerics
        foreach (var (vector, name) in ((string, string)[])[(Vector2, "Vector2"), (Vector3, "Vector3"), (Vector4, "Vector4")])
        {
            migration
                .CallToExpression(vector, "Floor", [vector], $"{name}.Round({{0}}, MidpointRounding.ToNegativeInfinity)", import: "System")
                .CallToExpression(vector, "Ceiling", [vector], $"{name}.Round({{0}}, MidpointRounding.ToPositiveInfinity)", import: "System");
        }

        // Transform by a matrix gave all four components in Stride
        migration
            .Move(Vector2, "Transform", [Vector2, Matrix], Vector4, Vector4)
            .Move(Vector3, "Transform", [Vector3, Matrix], Vector4, Vector4)
            .CastToCall(Vector2, Vector3, "AsVector3")
            .CastToCall(Vector2, Vector4, "AsVector4")
            .CastToCall(Vector3, Vector2, "AsVector2")
            .CastToCall(Vector3, Vector4, "AsVector4")
            .CastToCall(Vector4, Vector2, "AsVector2")
            .CastToCall(Vector4, Vector3, "AsVector3")
            .Rename(Vector4, "Moveto", "MoveTo")
            // A member that changed its vector is a function that returns the result
            .Helper(Vector3, "Pow")
            .Helper(Vector4, "Pow")
            .CallToExpression(Vector3, "NearEqual", [Vector3, Vector3, Vector3], "{0}.NearEquals({1}, {2})")
            // One way to the yaw, pitch and roll of a rotation is left
            .CallToExpression(Vector3, "RotationYawPitchRoll", [Quaternion], "{0}.GetYawPitchRoll()");

        migration
            // IsIdentity of System.Numerics is exact
            .MemberToExpression(Quaternion, "IsIdentity", "{0}.NearEquals(Quaternion.Identity)")
            .MemberToExpression(Quaternion, "One", "new Quaternion(1, 1, 1, 1)")
            // A value that is computed is a function, as Length() and GetDeterminant() of System.Numerics
            .MemberToExpression(Quaternion, "YawPitchRoll", "{0}.GetYawPitchRoll()")
            .MemberToExpression(Quaternion, "Angle", "{0}.GetAngle()")
            .MemberToExpression(Quaternion, "Axis", "{0}.GetAxis()")
            .ConstructorToExpression(Quaternion, ["float"], "new Quaternion({0}, {0}, {0}, {0})")
            .ConstructorToExpression(Quaternion, ["float[]"], "new Quaternion({0}[0], {0}[1], {0}[2], {0}[3])")
            .ConstructorToExpression(Quaternion, [Vector2, "float", "float"], "new Quaternion({0}.X, {0}.Y, {1}, {2})")
            .ConstructorToExpression(Quaternion, [Vector4], "{0}.AsQuaternion()")
            .FormatToCall(Quaternion, "ToString")
            .Rename(Quaternion, "Normalize", "NormalizeSafe")
            .Helper(Quaternion, "NormalizeSafe")
            .Rename(Quaternion, "Invert", "Inverse")
            .Rename(Quaternion, "RotationMatrix", "CreateFromRotationMatrix")
            // Named as the same functions of Matrix4x4
            .Rename(Quaternion, "RotationX", "CreateRotationX")
            .Rename(Quaternion, "RotationY", "CreateRotationY")
            .Rename(Quaternion, "RotationZ", "CreateRotationZ")
            .RefCallToAssignment(Quaternion, "Rotate", "Vector3.Transform({1}, {0})")
            // Stride gave the axis a length of one, which System.Numerics expects it to have; the unit axes and a normalized vector have it
            .CallToExpression(Quaternion, "RotationAxis", [Vector3, "float"], "Quaternion.CreateFromAxisAngle({0}, {1})", ["UnitX", "UnitY", "UnitZ", "Normalize"])
            .CallToExpression(Quaternion, "RotationAxis", [Vector3, "float"], "Quaternion.CreateFromAxisAngle(Vector3.NormalizeSafe({0}), {1})")
            .Rename(Quaternion, "RotationYawPitchRoll", "CreateFromYawPitchRoll", "float", "float", "float")
            .OutsToDeconstruction(Quaternion, "RotationYawPitchRoll", [Quaternion], "{0}.GetYawPitchRoll()")
            .Rename(Quaternion, "BetweenDirections", "CreateFromDirections")
            .Rename(Quaternion, "LookRotation", "CreateLookRotation")
            // The product of System.Numerics takes its operands the other way round; Concatenate takes them as Stride did
            .Rename(Quaternion, "Multiply", "Concatenate", Quaternion, Quaternion)
            .OperatorToCall(Quaternion, "op_Multiply", "Concatenate");

        migration
            .MemberToExpression(Matrix, "IsIdentity", "{0}.NearEquals(Matrix4x4.Identity)")
            // The layout is the one of System.Numerics, with nothing left to ask
            .MemberToExpression(Matrix, "LayoutIsRowMajor", "true")
            .MemberToExpression(Matrix, "Zero", "default(Matrix4x4)")
            // Only right for a matrix that scales and does nothing else
            .MemberToExpression(Matrix, "ScaleVector", "new Vector3({0}.M11, {0}.M22, {0}.M33)")
            .SetterToAssignment(Matrix, "ScaleVector", "{0} with { M11 = {1}.X, M22 = {1}.Y, M33 = {1}.Z }")
            .InPlaceToExpression(Matrix, "Orthogonalize", "Matrix4x4.Orthogonalize({0})")
            .InPlaceToExpression(Matrix, "Orthonormalize", "Matrix4x4.Orthonormalize({0})")
            .OperatorToExpression(Matrix, "op_UnaryPlus", "{0}")
            // Element by element, where the product is the one of matrices
            .OperatorToExpression(Matrix, "op_Division", "Matrix4x4.Create({0}.X / {1}.X, {0}.Y / {1}.Y, {0}.Z / {1}.Z, {0}.W / {1}.W)")
            .CallToExpression(Matrix, "Divide", [Matrix, Matrix], "Matrix4x4.Create({0}.X / {1}.X, {0}.Y / {1}.Y, {0}.Z / {1}.Z, {0}.W / {1}.W)")
            .Rename(Matrix, "Transformation", "CreateTransformation")
            .Rename(Matrix, "Transformation2D", "CreateTransformation2D")
            .Rename(Matrix, "AffineTransformation", "CreateAffineTransformation")
            .Rename(Matrix, "AffineTransformation2D", "CreateAffineTransformation2D")
            .Rename(Matrix, "MultiplyIn", "Multiply")
            .FormatToCall(Matrix, "ToString")
            .Rename(Matrix, "Determinant", "GetDeterminant")
            .Rename(Matrix, "LookAtRH", "CreateLookAt")
            .Rename(Matrix, "LookAtLH", "CreateLookAtLeftHanded")
            .Rename(Matrix, "OrthoRH", "CreateOrthographic")
            .Rename(Matrix, "OrthoLH", "CreateOrthographicLeftHanded")
            .Rename(Matrix, "OrthoOffCenterRH", "CreateOrthographicOffCenter")
            .Rename(Matrix, "OrthoOffCenterLH", "CreateOrthographicOffCenterLeftHanded")
            // The same for planes and an angle that give a projection; for the others System.Numerics throws, where Stride gave a matrix
            .Rename(Matrix, "PerspectiveRH", "CreatePerspective")
            .Rename(Matrix, "PerspectiveLH", "CreatePerspectiveLeftHanded")
            .Rename(Matrix, "PerspectiveFovRH", "CreatePerspectiveFieldOfView")
            .Rename(Matrix, "PerspectiveFovLH", "CreatePerspectiveFieldOfViewLeftHanded")
            .Rename(Matrix, "PerspectiveOffCenterRH", "CreatePerspectiveOffCenter")
            .Rename(Matrix, "PerspectiveOffCenterLH", "CreatePerspectiveOffCenterLeftHanded")
            .Rename(Matrix, "RotationQuaternion", "CreateFromQuaternion")
            // The same for the axis of length one and the normalized plane that Stride asked for
            .Rename(Matrix, "RotationAxis", "CreateFromAxisAngle")
            .Rename(Matrix, "Reflection", "CreateReflection")
            .Rename(Matrix, "RotationX", "CreateRotationX")
            .Rename(Matrix, "RotationY", "CreateRotationY")
            .Rename(Matrix, "RotationZ", "CreateRotationZ")
            .Rename(Matrix, "RotationYawPitchRoll", "CreateFromYawPitchRoll")
            .Rename(Matrix, "Scaling", "CreateScale")
            .Rename(Matrix, "Shadow", "CreateShadow")
            .Rename(Matrix, "Translation", "CreateTranslation")
            .Rename(Matrix, "Billboard", "CreateBillboard")
            .Rename(Matrix, "TranslationVector", "Translation")
            .Rename(Matrix, "Row1", "X")
            .Rename(Matrix, "Row2", "Y")
            .Rename(Matrix, "Row3", "Z")
            .Rename(Matrix, "Row4", "W")
            // A matrix that cannot be inverted gives Zero; Invert of System.Numerics gives NaN
            .ToOutCall(Matrix, "Invert", "InvertSafe", "Matrix4x4.Invert({0}, out var {result}) ? {result} : default", "inverse")
            .ConstructorToCall(Matrix, ["float"], "Create")
            .ConstructorToCall(Matrix, ["float[]"], "Create")
            .LinearIndexer(Matrix, 4);

        // Properties System.Numerics lacks: read through an extension property, written through a method
        foreach (var property in (string[])["Column1", "Column2", "Column3", "Column4", "Up", "Down", "Right", "Left", "Forward", "Backward"])
            migration.SetterToCall(Matrix, property, "Set" + property);

        // The double vectors stay, with the names and the forms of the vectors of System.Numerics
        const string Double3 = Namespace + "Double3";
        string[] doubleVectors = [Namespace + "Double2", Double3, Namespace + "Double4"];
        foreach (var type in doubleVectors)
        {
            migration
                .KeepType(type)
                .NewForms(type)
                .Rename(type, "Modulate", "Multiply")
                .Rename(type, "Demodulate", "Divide")
                // Zero stays zero; Normalize gives NaN now, as the one of System.Numerics
                .Rename(type, "Normalize", "NormalizeSafe")
                .Helper(type, "NormalizeSafe");
        }
        migration
            .Helper(Double3, "Pow")
            .Helper(Namespace + "Double4", "Pow")
            .CallToExpression(Double3, "NearEqual", [Double3, Double3, Double3], "{0}.NearEquals({1}, {2})");

        // The half of .NET is a number, which converts to float only when asked
        const string Half = Namespace + "Half";
        migration
            .Type(Half, "System.Half")
            .ImplicitToCast(Half, "float")
            .ConvertsFrom(Half, "byte", "sbyte")
            .ConstructorToExpression(Half, ["float"], "(Half){0}")
            .MemberToExpression(Half, "RawValue", "BitConverter.HalfToUInt16Bits({0})", "System")
            .SetterToAssignment(Half, "RawValue", "BitConverter.UInt16BitsToHalf({1})", "System")
            .CallToExpression(Half, "Equals", [Half, Half], "{0}.Equals({1})")
            .CallToExpression(Half, "ConvertToFloat", [Half + "[]"], "Array.ConvertAll({0}, x => (float)x)", import: "System")
            .CallToExpression(Half, "ConvertToHalf", ["float[]"], "Array.ConvertAll({0}, x => (Half)x)", import: "System")
            // System.Half gives other values under these names: the numbers Stride had
            .MemberToExpression(Half, "Epsilon", "0.0004887581f")
            .MemberToExpression(Half, "MaxValue", "65504f")
            .MemberToExpression(Half, "MinValue", "6.103516E-05f");
        foreach (var (name, value) in ((string, int)[])[("PrecisionDigits", 3), ("MantissaBits", 11), ("MaximumDecimalExponent", 4), ("MaximumBinaryExponent", 15),
            ("MinimumDecimalExponent", -4), ("MinimumBinaryExponent", -14), ("ExponentRadix", 2), ("AdditionRounding", 1)])
        {
            migration.MemberToExpression(Half, name, value.ToString(CultureInfo.InvariantCulture));
        }

        // So do the vectors of integers and of halves
        string[] integerVectors = [Namespace + "Int2", Namespace + "Int3", Namespace + "Int4", Namespace + "UInt4"];
        foreach (var type in integerVectors)
        {
            migration
                .KeepType(type)
                .NewForms(type)
                .Rename(type, "Modulate", "Multiply");
        }
        migration
            .Helper(Namespace + "Int2", "Pow")
            .Helper(Namespace + "Int3", "Pow");
        foreach (var type in (string[])[Namespace + "Half2", Namespace + "Half3", Namespace + "Half4"])
        {
            migration
                .KeepType(type)
                .NewForms(type)
                // One comparison of two values is left
                .CallToExpression(type, "Equals", [type, type], "{0}.Equals({1})");
        }

        migration
            // D is Dot(normal, point), as the intersection tests of Stride expect
            .ConstructorToExpression(Plane, [Vector3, Vector3], "new Plane({1}, Vector3.Dot({1}, {0}))")
            .ConstructorToCall(Plane, [Vector3, Vector3, Vector3], "CreateFromVertices")
            .ConstructorToExpression(Plane, ["float"], "new Plane({0}, {0}, {0}, {0})")
            .ConstructorToExpression(Plane, ["float[]"], "new Plane({0}[0], {0}[1], {0}[2], {0}[3])")
            .CallToExpression(Plane, "Normalize", ["float", "float", "float", "float"], "Plane.Normalize(new Plane({0}, {1}, {2}, {3}))")
            // The operators say the same
            .CallToExpression(Plane, "Negate", [Plane], "-{0}")
            .InPlaceToExpression(Plane, "Negate", "-{0}")
            .CallToExpression(Plane, "Multiply", [Plane, "float"], "{0} * {1}")
            .IndexerToMembers(Plane, ["Normal.X", "Normal.Y", "Normal.Z", "D"], "{0}.AsVector4()[{1}]")
            .FormatToCall(Plane, "ToString");

        return migration;
    }

    /// <summary>
    /// Migrates code from types that were replaced by the types of another library with a close API, as
    /// <paramref name="migration"/> describes. Besides what it lists, a call takes the form the new type has, when that
    /// type has the method in no other form:
    /// <list type="bullet">
    /// <item><c>T.M(ref a, ref b, out c);</c> → <c>c = T.M(a, b);</c></item>
    /// <item><c>x.M(a);</c>, which changes <c>x</c> → <c>x = T.M(x, a);</c></item>
    /// <item><c>T.M(ref a)</c> or <c>T.M(in a)</c>, where the old method only reads <c>a</c> → <c>T.M(a)</c>: the new
    /// library takes such a value by value or by <c>in</c></item>
    /// </list>
    /// A file that names a replaced type gets the <c>using</c> of its new namespace. A member that the new type does
    /// not have is taken for an extension member declared in the namespace of the old type: a file that uses one keeps or
    /// gets that <c>using</c>, and a static one named alone (through a <c>using static</c>) gets the name of its type.
    /// </summary>
    public static CodeUpgrade MigrateTypes(TypeMigration migration)
    {
        ArgumentNullException.ThrowIfNull(migration);
        return async (solution, targets, cancellationToken) =>
        {
            // Every project is read as it was: a project rewritten first no longer has the old types its dependents name
            var original = solution;
            foreach (var projectId in targets)
            {
                var project = original.GetProject(projectId);
                if (project is null)
                    continue;
                var compilation = await project.GetCompilationAsync(cancellationToken);
                if (compilation is null)
                    continue;

                var types = new Dictionary<INamedTypeSymbol, string>(SymbolEqualityComparer.Default);
                foreach (var (oldType, newType) in migration.Types)
                {
                    if (compilation.GetTypeByMetadataName(oldType) is { } symbol)
                        types[symbol] = newType;
                }
                if (types.Count == 0)
                    continue;

                foreach (var document in project.Documents)
                {
                    if (!SymbolRewriteEngine.IsUpgradableSource(document) || await document.GetSyntaxRootAsync(cancellationToken) is not CompilationUnitSyntax root)
                        continue;
                    var semanticModel = compilation.GetSemanticModel(root.SyntaxTree);
                    var rewriter = new TypeMigrationRewriter(migration, types, semanticModel, cancellationToken);
                    var newRoot = (CompilationUnitSyntax)rewriter.Visit(root);
                    // A document that only calls members through var or other members still needs the using of an extension member
                    if (newRoot == root && rewriter.NamespacesToImport.Count == 0)
                        continue;
                    foreach (var @namespace in rewriter.NamespacesToImport)
                        newRoot = AddUsing(newRoot, @namespace);
                    solution = solution.WithDocumentSyntaxRoot(document.Id, newRoot);
                }
            }
            return solution;
        };
    }

    /// <summary>
    /// Rewrites one document. Every decision reads the semantic model at the original node; the edits apply to the node whose
    /// children are already rewritten, so they nest.
    /// </summary>
    private sealed class TypeMigrationRewriter(TypeMigration migration, Dictionary<INamedTypeSymbol, string> types, SemanticModel semanticModel, CancellationToken cancellationToken)
        : CSharpSyntaxRewriter(visitIntoStructuredTrivia: true)
    {
        /// <summary>The namespaces of the new types this document now names without qualifying them.</summary>
        public readonly SortedSet<string> NamespacesToImport = new(StringComparer.Ordinal);

        private ISymbol SymbolOf(SyntaxNode node) => semanticModel.GetSymbolInfo(node, cancellationToken).Symbol;

        private bool IsMigrated(ITypeSymbol type) => type is INamedTypeSymbol named && types.ContainsKey(named);

        // A type the new library has under another name: not one that stays and only has rules for its members
        private bool IsReplaced(INamedTypeSymbol type) => types.TryGetValue(type, out var newType) && newType != FullName(type);

        // A type whose calls take the forms of the new library: a replaced one, or one that stays and has these forms now
        private bool HasNewForms(INamedTypeSymbol type) => IsReplaced(type) || migration.NewFormTypes.Contains(FullName(type));

        // Stride.Core.Mathematics.Vector3, float, float[]
        private static readonly SymbolDisplayFormat FullNameFormat = new(
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
            miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

        private static string FullName(ITypeSymbol type) => type.ToDisplayString(FullNameFormat);

        // The new type, named without its namespace, which the document then imports unless it already resolves there
        private IdentifierNameSyntax NewTypeName(INamedTypeSymbol oldType, SyntaxNode location)
        {
            var newType = types[oldType];
            var separator = newType.LastIndexOf('.');
            var @namespace = newType[..separator];
            if (!IsImported(@namespace, location))
                NamespacesToImport.Add(@namespace);
            return SyntaxFactory.IdentifierName(newType[(separator + 1)..]);
        }

        // The parameters a call gives values for: all of them, less a last out parameter that carries the result
        private static IEnumerable<IParameterSymbol> ValueParameters(IMethodSymbol method)
            => method.ReturnsVoid && method.Parameters is [.., { RefKind: RefKind.Out }] ? method.Parameters[..^1] : method.Parameters;

        // The replaced type that has the member in the new library, when another one than the member's own
        private INamedTypeSymbol MovedTo(ISymbol member, out string newName)
        {
            newName = null;
            if (member is not IMethodSymbol { IsStatic: true } method || !IsMigrated(method.ContainingType))
                return null;
            var typeName = FullName(method.ContainingType);
            var parameters = ValueParameters(method).Select(x => FullName(x.Type));
            var result = FullName(method.ReturnsVoid && method.Parameters is [.., { RefKind: RefKind.Out } last] ? last.Type : method.ReturnType);
            foreach (var move in migration.Moves)
            {
                if (move.Type == typeName && move.Member == method.Name && move.ResultType == result && parameters.SequenceEqual(move.ParameterTypes))
                {
                    newName = move.NewMember;
                    return types.Keys.FirstOrDefault(x => FullName(x) == move.NewType);
                }
            }
            return null;
        }

        private string NewMemberName(ISymbol member)
        {
            if (member?.ContainingType is not { } type || !IsMigrated(type.OriginalDefinition))
                return null;
            if (MovedTo(member, out var movedName) is not null)
                return movedName == member.Name ? null : movedName;
            if (member is IMethodSymbol outMethod && OutCallOf(outMethod.ReducedFrom ?? outMethod) is { } outCall)
                return outCall.Method;
            var typeName = FullName(type.OriginalDefinition);
            foreach (var rename in migration.Renames)
            {
                if (rename.Type != typeName || rename.Member != member.Name)
                    continue;
                if (rename.ParameterTypes is null
                    || (member is IMethodSymbol method && ValueParameters(method.ReducedFrom ?? method).Select(x => FullName(x.Type)).SequenceEqual(rename.ParameterTypes)))
                    return rename.NewMember;
            }
            return null;
        }

        private string NewFullName(ITypeSymbol type) => type is INamedTypeSymbol named && types.TryGetValue(named, out var replaced) ? replaced : FullName(type);

        // Whether the new library has, in place of this method, a static method that takes these values and returns that one
        private bool NewLibraryHas(IMethodSymbol method, IEnumerable<ITypeSymbol> parameters, ITypeSymbol result)
        {
            var owner = MovedTo(method, out _) ?? method.ContainingType;
            var name = NewMemberName(method) ?? method.Name;
            if (migration.Helpers.Contains((FullName(owner), name)))
                return true;
            if (semanticModel.Compilation.GetTypeByMetadataName(types[owner]) is not { } newType)
                return false;
            var newParameters = parameters.Select(NewFullName).ToList();
            return newType.GetMembers(name).OfType<IMethodSymbol>().Any(x =>
                x.IsStatic && FullName(x.ReturnType) == NewFullName(result)
                && x.Parameters.All(p => p.RefKind is not (RefKind.Out or RefKind.Ref))
                && x.Parameters.Select(p => FullName(p.Type)).SequenceEqual(newParameters));
        }

        // The names of the calls that take another form: the method they call then is not the one they name now
        private readonly HashSet<SyntaxNode> convertedCalls = [];

        // A member the new type does not have is an extension member, declared in the namespace of the old type: the
        // document needs that namespace to see it
        private void ImportExtensionMember(INamedTypeSymbol oldType, string name, SyntaxNode location, IMethodSymbol method = null)
        {
            if (!IsReplaced(oldType) || (!migration.Helpers.Contains((FullName(oldType), name)) && NewTypeHas(oldType, name, method)))
                return;
            var @namespace = oldType.ContainingNamespace.ToDisplayString();
            if (!IsImported(@namespace, location))
                NamespacesToImport.Add(@namespace);
        }

        // Whether the new type has a member of this name; for a method, one that takes the same parameters
        private bool NewTypeHas(INamedTypeSymbol oldType, string name, IMethodSymbol method)
        {
            if (semanticModel.Compilation.GetTypeByMetadataName(types[oldType]) is not { } newType)
                return true;
            var members = newType.GetMembers(name);
            if (method is null)
                return !members.IsEmpty;
            var parameters = method.Parameters.Select(x => (NewFullName(x.Type), IsWritten(x))).ToList();
            return members.OfType<IMethodSymbol>().Any(x => x.Parameters.Select(p => (FullName(p.Type), IsWritten(p))).SequenceEqual(parameters));

            static bool IsWritten(IParameterSymbol parameter) => parameter.RefKind is RefKind.Ref or RefKind.Out;
        }

        // An expression that converts by itself gets the cast written, where the conversions of the new type would make
        // the code mean something else, or nothing
        public override SyntaxNode Visit(SyntaxNode node)
        {
            var visited = base.Visit(node);
            if ((migration.ImplicitCasts.Count == 0 && migration.NewConversions.Count == 0) || node is not ExpressionSyntax original || visited is not ExpressionSyntax expression)
                return visited;
            // The name of a member and an expression in parentheses have the types of the expression around them, which is
            // the one that converts
            if (node.Parent is ParenthesizedExpressionSyntax || (node is SimpleNameSyntax && node.Parent is MemberAccessExpressionSyntax or MemberBindingExpressionSyntax or QualifiedNameSyntax))
                return visited;
            var info = semanticModel.GetTypeInfo(original, cancellationToken);
            if (info.Type is null || info.ConvertedType is null || SymbolEqualityComparer.Default.Equals(info.Type, info.ConvertedType))
                return visited;
            if ((ImplicitCastOf(original, info.Type) ?? AmbiguousParameterOf(original)) is not { } target)
                return visited;

            // 1 → 1f, rather than (float)1
            if (target.SpecialType == SpecialType.System_Single
                && (expression as PrefixUnaryExpressionSyntax)?.Operand is var operand && (operand ?? expression) is LiteralExpressionSyntax { Token.Text: var digits } literal
                && literal.IsKind(SyntaxKind.NumericLiteralExpression) && digits.All(char.IsAsciiDigit)
                && (operand is null || expression.IsKind(SyntaxKind.UnaryMinusExpression)))
            {
                var number = SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(digits + "f", float.Parse(digits, CultureInfo.InvariantCulture)));
                return expression.ReplaceNode(literal, number.WithTriviaFrom(literal));
            }
            return SyntaxFactory.CastExpression(SyntaxFactory.ParseTypeName(FullName(target)), AsOperand(expression.WithoutTrivia())).WithTriviaFrom(expression);
        }

        // x → (To)x, where the old type converted by itself and the new one only when asked
        private ITypeSymbol ImplicitCastOf(ExpressionSyntax expression, ITypeSymbol type)
        {
            if (type is not INamedTypeSymbol from || !IsReplaced(from))
                return null;
            var conversion = semanticModel.GetConversion(expression, cancellationToken);
            return conversion.IsImplicit && conversion.MethodSymbol is { MethodKind: MethodKind.Conversion } method
                && migration.ImplicitCasts.Contains((FullName(from), FullName(method.ReturnType))) ? method.ReturnType : null;
        }

        // M(b) → M((P)b), where M also takes the replaced type there and the new type converts from b by itself: the call
        // would no longer choose between the two
        private ITypeSymbol AmbiguousParameterOf(ExpressionSyntax expression)
        {
            if (migration.NewConversions.Count == 0 || expression.Parent is not ArgumentSyntax { RefKindKeyword.RawKind: (int)SyntaxKind.None } argument
                || semanticModel.GetOperation(argument, cancellationToken) is not IArgumentOperation { Parameter: { ContainingSymbol: IMethodSymbol { ContainingType: { } owner } method } parameter })
                return null;
            foreach (var other in owner.GetMembers(method.Name).OfType<IMethodSymbol>())
            {
                if (SymbolEqualityComparer.Default.Equals(other, method) || other.Parameters.Length != method.Parameters.Length
                    || other.Parameters[parameter.Ordinal].Type is not INamedTypeSymbol replaced || !IsReplaced(replaced))
                    continue;
                var replacedName = FullName(replaced);
                foreach (var source in (SpecialType[])[SpecialType.System_Byte, SpecialType.System_SByte, SpecialType.System_Int16, SpecialType.System_UInt16, SpecialType.System_Int32])
                {
                    var sourceType = semanticModel.Compilation.GetSpecialType(source);
                    // A constant converts to a smaller integer by itself when it fits
                    if (migration.NewConversions.Contains((replacedName, FullName(sourceType))) && semanticModel.ClassifyConversion(expression, sourceType).IsImplicit)
                        return parameter.Type;
                }
            }
            return null;
        }

        public override SyntaxNode VisitIdentifierName(IdentifierNameSyntax node)
        {
            // var binds to the type it infers; the name of an argument is not looked up by itself
            if (node.IsVar || node.Parent is NameColonSyntax or NameEqualsSyntax)
                return node;

            var symbol = SymbolOf(node);
            switch (symbol)
            {
                // Through an alias, the name stays: its using directive is the place that changes
                case INamedTypeSymbol type when IsReplaced(type) && semanticModel.GetAliasInfo(node, cancellationToken) is null
                    && !(node.Parent is QualifiedNameSyntax qualified && qualified.Right == node)
                    && !(node.Parent is MemberAccessExpressionSyntax access && access.Name == node):
                    return NewTypeName(type, node).WithTriviaFrom(node);

                case IMethodSymbol { MethodKind: MethodKind.Ordinary or MethodKind.ReducedExtension } or IPropertySymbol or IFieldSymbol:
                {
                    var member = (symbol as IMethodSymbol)?.ReducedFrom ?? symbol;
                    if (member.ContainingType is not { } owner || !IsMigrated(owner))
                        break;
                    var newName = NewMemberName(member);
                    var newOwner = MovedTo(member, out _) ?? owner;
                    if (!convertedCalls.Contains(node))
                        ImportExtensionMember(newOwner, newName ?? member.Name, node, member as IMethodSymbol);

                    var name = newName is null ? node : node.WithIdentifier(SyntaxFactory.Identifier(node.Identifier.LeadingTrivia, newName, node.Identifier.TrailingTrivia));
                    // A static member named alone, through a using static: an extension member is not found that way
                    if (member.IsStatic && !(node.Parent is MemberAccessExpressionSyntax qualifier && qualifier.Name == node) && !node.IsPartOfStructuredTrivia()
                        && !SymbolEqualityComparer.Default.Equals(semanticModel.GetEnclosingSymbol(node.SpanStart, cancellationToken)?.ContainingType, owner))
                    {
                        return SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, NewTypeName(newOwner, node), name.WithoutTrivia()).WithTriviaFrom(node);
                    }
                    return name;
                }
            }
            return node;
        }

        // Namespace.Type in a type position
        public override SyntaxNode VisitQualifiedName(QualifiedNameSyntax node)
        {
            if (SymbolOf(node) is INamedTypeSymbol type && IsReplaced(type))
                return SyntaxFactory.ParseName(types[type]).WithTriviaFrom(node);
            return base.VisitQualifiedName(node);
        }

        // Namespace.Type in a documentation comment
        public override SyntaxNode VisitQualifiedCref(QualifiedCrefSyntax node)
        {
            if (SymbolOf(node) is INamedTypeSymbol type && IsReplaced(type))
                return SyntaxFactory.NameMemberCref(SyntaxFactory.ParseName(types[type])).WithTriviaFrom(node);
            return base.VisitQualifiedCref(node);
        }

        // Whether the names of a namespace resolve unqualified at the node
        private bool IsImported(string @namespace, SyntaxNode location)
        {
            var enclosing = semanticModel.GetEnclosingSymbol(location.SpanStart, cancellationToken);
            for (var current = enclosing as INamespaceSymbol ?? enclosing?.ContainingNamespace; current is { IsGlobalNamespace: false }; current = current.ContainingNamespace)
            {
                if (current.ToDisplayString() == @namespace)
                    return true;
            }
            return semanticModel.GetImportScopes(location.SpanStart, cancellationToken)
                .Any(scope => scope.Imports.Any(x => x.NamespaceOrType is INamespaceSymbol imported && imported.ToDisplayString() == @namespace));
        }

        public override SyntaxNode VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
        {
            // Namespace.Type in an expression
            if (SymbolOf(node) is INamedTypeSymbol type && IsReplaced(type))
                return SyntaxFactory.ParseExpression(types[type]).WithTriviaFrom(node);

            // x.P → the expression the migration gives for P
            if (node.IsKind(SyntaxKind.SimpleMemberAccessExpression) && SymbolOf(node) is (IPropertySymbol or IFieldSymbol) and { ContainingType: { } owningType } member
                && IsReplaced(owningType) && !IsWritten(node))
            {
                var typeName = FullName(owningType);
                var rule = migration.MemberExpressions.FirstOrDefault(x => x.Type == typeName && x.Member == member.Name);
                if (rule.Expression is not null)
                {
                    NewTypeName(owningType, node);
                    if (rule.Import is not null && !IsImported(rule.Import, node))
                        NamespacesToImport.Add(rule.Import);
                    if (member.IsStatic)
                        return SyntaxFactory.ParseExpression(rule.Expression).WithTriviaFrom(node);
                    ImportNamespaceOf(owningType, node);
                    return InPlaceOf(node, FromTemplate(rule.Expression, [(ExpressionSyntax)Visit(node.Expression)], node, "value")).WithTriviaFrom(node);
                }
            }

            // A.M → B.N, for a static method that another type has now
            if (SymbolOf(node.Expression) is INamedTypeSymbol && MovedTo(SymbolOf(node.Name), out _) is { } owner)
                return node.Update(NewTypeName(owner, node).WithTriviaFrom(node.Expression), node.OperatorToken, (SimpleNameSyntax)Visit(node.Name));

            return base.VisitMemberAccessExpression(node);
        }

        // m[i] → m[i / columns, i % columns]
        public override SyntaxNode VisitElementAccessExpression(ElementAccessExpressionSyntax node)
        {
            if (SymbolOf(node) is IPropertySymbol { IsIndexer: true, Parameters: [{ Type.SpecialType: SpecialType.System_Int32 }] } memberIndexer
                && IsReplaced(memberIndexer.ContainingType) && migration.Indexers.TryGetValue(FullName(memberIndexer.ContainingType), out var members)
                && node.ArgumentList.Arguments is [{ NameColon: null, Expression: var memberIndex }])
            {
                if (semanticModel.GetConstantValue(memberIndex, cancellationToken) is { HasValue: true, Value: int constantIndex } && constantIndex >= 0 && constantIndex < members.Members.Length)
                    return FromTemplate("{0}." + members.Members[constantIndex], [(ExpressionSyntax)Visit(node.Expression)], node).WithTriviaFrom(node);
                if (!IsWritten(node))
                {
                    NewTypeName(memberIndexer.ContainingType, node);
                    return FromTemplate(members.Read, [(ExpressionSyntax)Visit(node.Expression), (ExpressionSyntax)Visit(memberIndex)], node, "value", "index").WithTriviaFrom(node);
                }
            }

            var visited = (ElementAccessExpressionSyntax)base.VisitElementAccessExpression(node);
            if (SymbolOf(node) is not IPropertySymbol { IsIndexer: true, Parameters: [{ Type.SpecialType: SpecialType.System_Int32 }] } indexer
                || !IsMigrated(indexer.ContainingType) || !migration.LinearIndexers.TryGetValue(FullName(indexer.ContainingType), out var columns)
                || node.ArgumentList.Arguments is not [{ NameColon: null, Expression: var index }] || !IsRepeatable(index))
                return visited;

            ExpressionSyntax row, column;
            if (semanticModel.GetConstantValue(index, cancellationToken) is { HasValue: true, Value: int constant })
            {
                row = Literal(constant / columns);
                column = Literal(constant % columns);
            }
            else
            {
                var operand = visited.ArgumentList.Arguments[0].Expression.WithoutTrivia();
                if (operand is not (IdentifierNameSyntax or LiteralExpressionSyntax or ParenthesizedExpressionSyntax or MemberAccessExpressionSyntax))
                    operand = SyntaxFactory.ParenthesizedExpression(operand);
                row = Binary(SyntaxKind.DivideExpression, SyntaxKind.SlashToken, operand, Literal(columns));
                column = Binary(SyntaxKind.ModuloExpression, SyntaxKind.PercentToken, operand, Literal(columns));
            }
            var arguments = SyntaxFactory.SeparatedList([SyntaxFactory.Argument(row), SyntaxFactory.Argument(column)],
                [SyntaxFactory.Token(SyntaxKind.CommaToken).WithTrailingTrivia(SyntaxFactory.Space)]);
            return visited.WithArgumentList(visited.ArgumentList.WithArguments(arguments));

            static LiteralExpressionSyntax Literal(int value) => SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(value));

            static BinaryExpressionSyntax Binary(SyntaxKind kind, SyntaxKind @operator, ExpressionSyntax left, ExpressionSyntax right)
                => SyntaxFactory.BinaryExpression(kind, left, SyntaxFactory.Token(@operator).WithLeadingTrivia(SyntaxFactory.Space).WithTrailingTrivia(SyntaxFactory.Space), right);
        }

        public override SyntaxNode VisitExpressionStatement(ExpressionStatementSyntax node)
        {
            // b = T.M(a); → T.Method(a, out b);
            if (node.Expression is AssignmentExpressionSyntax { RawKind: (int)SyntaxKind.SimpleAssignmentExpression, Left: var result, Right: InvocationExpressionSyntax assigned }
                && SymbolOf(assigned) is IMethodSymbol { IsStatic: true, ReturnsVoid: false } assignedMethod && OutCallOf(assignedMethod) is { } assignedRule
                && CanBeOut(result, assignedMethod.ReturnType) && ValueArguments(assigned, assignedMethod) is { } assignedValues)
            {
                var call = OutCall(assignedMethod.ContainingType, assigned, assignedRule.Method, assignedValues, (ExpressionSyntax)Visit(result));
                return SyntaxFactory.ExpressionStatement(call).WithTriviaFrom(node);
            }

            // T.M(a); → T.Method(a, out _);
            if (node.Expression is InvocationExpressionSyntax unused && SymbolOf(unused) is IMethodSymbol { IsStatic: true, ReturnsVoid: false } unusedMethod
                && OutCallOf(unusedMethod) is { } unusedRule && ValueArguments(unused, unusedMethod) is { } unusedValues)
            {
                var call = OutCall(unusedMethod.ContainingType, unused, unusedRule.Method, unusedValues, SyntaxFactory.IdentifierName("_"));
                return SyntaxFactory.ExpressionStatement(call).WithTriviaFrom(node);
            }

            if (node.Expression is InvocationExpressionSyntax invocation && SymbolOf(invocation) is IMethodSymbol { ReturnsVoid: true } method)
            {
                method = method.ReducedFrom ?? method;
                // The form of a call only changes for a type that has the forms of the new library; a method with an out rule has its own form
                if (HasNewForms(method.ContainingType) && OutCallOf(method) is null)
                {
                    var rewritten = OutsToDeconstruction(node, invocation, method) ?? OutToAssignment(node, invocation, method) ?? RefToAssignment(node, invocation, method)
                        ?? InPlaceToExpression(node, invocation, method) ?? InPlaceToAssignment(node, invocation, method);
                    if (rewritten is not null)
                        return rewritten;
                }
            }

            // x.P = value; → x = the expression the migration gives for P, of x and value
            if (node.Expression is AssignmentExpressionSyntax { RawKind: (int)SyntaxKind.SimpleAssignmentExpression, Left: MemberAccessExpressionSyntax written, Right: var writtenValue }
                && written.IsKind(SyntaxKind.SimpleMemberAccessExpression) && SymbolOf(written) is IPropertySymbol writtenProperty && IsMigrated(writtenProperty.ContainingType)
                && migration.SetterExpressions.TryGetValue((FullName(writtenProperty.ContainingType), writtenProperty.Name), out var setterRule) && IsRepeatable(written.Expression))
            {
                if (setterRule.Import is not null && !IsImported(setterRule.Import, node))
                    NamespacesToImport.Add(setterRule.Import);
                var owner = ((ExpressionSyntax)Visit(written.Expression)).WithoutTrivia();
                var newValue = FromTemplate(setterRule.Expression, [owner, (ExpressionSyntax)Visit(writtenValue)], node, "target", "value");
                return SyntaxFactory.ExpressionStatement(Assignment(owner, newValue)).WithTriviaFrom(node);
            }

            // x.P = value; → x.Method(value);
            if (node.Expression is AssignmentExpressionSyntax { RawKind: (int)SyntaxKind.SimpleAssignmentExpression, Left: MemberAccessExpressionSyntax target } assignment
                && SymbolOf(target) is IPropertySymbol property && IsMigrated(property.ContainingType)
                && migration.Setters.TryGetValue((FullName(property.ContainingType), property.Name), out var setter))
            {
                ImportExtensionMember(property.ContainingType, setter, node);
                var call = SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, ((ExpressionSyntax)Visit(target.Expression)).WithoutTrivia(), SyntaxFactory.IdentifierName(setter)),
                    SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(((ExpressionSyntax)Visit(assignment.Right)).WithoutTrivia()))));
                return SyntaxFactory.ExpressionStatement(call).WithTriviaFrom(node);
            }
            return base.VisitExpressionStatement(node);
        }

        // var b = T.M(a); → T.Method(a, out var b);
        public override SyntaxNode VisitLocalDeclarationStatement(LocalDeclarationStatementSyntax node)
        {
            if (node.Modifiers.Count == 0 && node.UsingKeyword.IsKind(SyntaxKind.None)
                && node.Declaration is { Type: var type, Variables: [{ Initializer.Value: InvocationExpressionSyntax invocation } variable] }
                && SymbolOf(invocation) is IMethodSymbol { IsStatic: true, ReturnsVoid: false } method && OutCallOf(method) is { } rule
                && SymbolEqualityComparer.Default.Equals(semanticModel.GetTypeInfo(type, cancellationToken).Type, method.ReturnType)
                && ValueArguments(invocation, method) is { } values)
            {
                var declaration = SyntaxFactory.DeclarationExpression(((TypeSyntax)Visit(type)).WithoutTrivia().WithTrailingTrivia(SyntaxFactory.Space),
                    SyntaxFactory.SingleVariableDesignation(variable.Identifier.WithoutTrivia()));
                return SyntaxFactory.ExpressionStatement(OutCall(method.ContainingType, invocation, rule.Method, values, declaration)).WithTriviaFrom(node);
            }
            return base.VisitLocalDeclarationStatement(node);
        }

        public override SyntaxNode VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            if (SymbolOf(node) is IMethodSymbol symbol && (symbol.ReducedFrom ?? symbol) is var method && OutCallOf(method) is { } rule)
            {
                var rewritten = method.ReturnsVoid ? VoidToOutCall(node, method, rule.Method) : ResultExpression(node, method, rule.Expression, rule.ResultName);
                if (rewritten is not null)
                    return rewritten.WithTriviaFrom(node);
            }

            // T.M(a) → the expression the migration gives for M
            if (SymbolOf(node) is IMethodSymbol { IsStatic: true, ReturnsVoid: false } called && CallExpression(called, node.ArgumentList.Arguments.FirstOrDefault()?.Expression) is { } callRule
                && ValueArguments(node, called) is { } arguments)
            {
                NewTypeName(called.ContainingType, node);
                return InPlaceOf(node, FromCallRule(callRule, called, arguments, node)).WithTriviaFrom(node);
            }
            return base.VisitInvocationExpression(node);
        }

        // The expression the migration gives for the calls of a static method, by the values it takes, and the namespace it needs
        private (string Expression, string Import)? CallExpression(IMethodSymbol method, ExpressionSyntax firstArgument)
        {
            if (method.ContainingType is not { } type || !HasNewForms(type))
                return null;
            var typeName = FullName(type);
            var parameters = ValueParameters(method).Select(x => FullName(x.Type)).ToList();
            // The static member of its type that gives the first argument, for the rules that are only for some of them
            var firstMember = firstArgument is not null && parameters is [var firstType, ..]
                && SymbolOf(firstArgument) is (IFieldSymbol or IPropertySymbol or IMethodSymbol) and { IsStatic: true } member && FullName(member.ContainingType) == firstType
                ? member.Name : null;
            foreach (var rule in migration.CallExpressions)
            {
                if (rule.Type == typeName && rule.Method == method.Name && rule.ParameterTypes.SequenceEqual(parameters)
                    && (rule.FirstArguments is null || rule.FirstArguments.Contains(firstMember)))
                    return (rule.Expression, rule.Import);
            }
            return null;
        }

        // The expression of a call rule with these values, and its namespaces imported
        private ExpressionSyntax FromCallRule((string Expression, string Import) rule, IMethodSymbol method, IReadOnlyList<ExpressionSyntax> values, SyntaxNode location)
        {
            ImportNamespaceOf(method.ContainingType, location);
            if (rule.Import is not null && !IsImported(rule.Import, location))
                NamespacesToImport.Add(rule.Import);
            return FromTemplate(rule.Expression, values, location, [.. ValueParameters(method).Select(x => x.Name)]);
        }

        // An expression the migration gives, with each value where it names it: {0} is the first one. A value it names
        // more than once goes through a variable, unless writing it twice reads the same value twice.
        private ExpressionSyntax FromTemplate(string template, IReadOnlyList<ExpressionSyntax> values, SyntaxNode location, params string[] names)
        {
            for (int i = 0; i < values.Count; i++)
                template = template.Replace("{" + i + "}", Placeholder(i));
            var expression = SyntaxFactory.ParseExpression(template);
            var placeholders = expression.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>().Where(x => x.Identifier.ValueText.StartsWith(PlaceholderPrefix, StringComparison.Ordinal)).ToList();
            values = [.. values.Select(x => x.WithoutTrivia())];
            if (placeholders.GroupBy(PlaceholderIndex).All(x => x.Count() == 1 || IsRepeatable(values[x.Key])))
            {
                return expression.ReplaceNodes(placeholders, (placeholder, _) =>
                {
                    var value = values[PlaceholderIndex(placeholder)];
                    return (placeholder.Parent is ArgumentSyntax ? value : AsOperand(value)).WithTriviaFrom(placeholder);
                });
            }

            // Otherwise through variables, set once and in the order of the values
            var variables = values.Select((_, i) => UnusedName(i < names.Length ? names[i] : "value", location)).ToList();
            expression = expression.ReplaceNodes(placeholders, (placeholder, _) => SyntaxFactory.IdentifierName(variables[PlaceholderIndex(placeholder)]).WithTriviaFrom(placeholder));
            var source = string.Join(", ", values.Select(x => x.ToFullString()));
            return SyntaxFactory.ParseExpression(values.Count == 1
                ? $"({source} is var {variables[0]} ? {expression} : default)"
                : $"(({source}) is var ({string.Join(", ", variables)}) ? {expression} : default)");
        }

        // An expression that takes the place of another one, in parentheses where what is around it would bind inside it
        private static ExpressionSyntax InPlaceOf(ExpressionSyntax original, ExpressionSyntax replacement)
        {
            var standsAlone = original.Parent is ArgumentSyntax or EqualsValueClauseSyntax or ReturnStatementSyntax or ArrowExpressionClauseSyntax or ParenthesizedExpressionSyntax
                or LambdaExpressionSyntax or ExpressionStatementSyntax or InterpolationSyntax
                || (original.Parent is AssignmentExpressionSyntax assignment && assignment.Right == original);
            return standsAlone ? replacement : AsOperand(replacement);
        }

        // The namespace of the old type has the extension members of the new one
        private void ImportNamespaceOf(INamedTypeSymbol oldType, SyntaxNode location)
        {
            var @namespace = oldType.ContainingNamespace.ToDisplayString();
            if (!IsImported(@namespace, location))
                NamespacesToImport.Add(@namespace);
        }

        // Whether the code assigns the expression or gives it by reference
        private static bool IsWritten(ExpressionSyntax expression) => expression.Parent switch
        {
            AssignmentExpressionSyntax assignment => assignment.Left == expression,
            PrefixUnaryExpressionSyntax prefix => prefix.IsKind(SyntaxKind.PreIncrementExpression) || prefix.IsKind(SyntaxKind.PreDecrementExpression),
            PostfixUnaryExpressionSyntax => true,
            ArgumentSyntax argument => argument.RefKindKeyword.IsKind(SyntaxKind.RefKeyword) || argument.RefKindKeyword.IsKind(SyntaxKind.OutKeyword),
            _ => false,
        };

        private (string Method, string Expression, string ResultName)? OutCallOf(IMethodSymbol method)
        {
            if (method.ContainingType is not { } type || !IsReplaced(type))
                return null;
            var typeName = FullName(type);
            foreach (var rule in migration.OutCalls)
            {
                if (rule.Type == typeName && rule.Member == method.Name)
                    return (rule.Method, rule.Expression, rule.ResultName);
            }
            return null;
        }

        // T.Method(a, out result)
        private InvocationExpressionSyntax OutCall(INamedTypeSymbol type, SyntaxNode location, string method, IEnumerable<ExpressionSyntax> values, ExpressionSyntax result)
        {
            ImportExtensionMember(type, method, location);
            var arguments = values.Select(x => SyntaxFactory.Argument(x.WithoutTrivia()))
                .Append(SyntaxFactory.Argument(null, SyntaxFactory.Token(SyntaxKind.OutKeyword).WithTrailingTrivia(SyntaxFactory.Space), result.WithoutTrivia())).ToList();
            return SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, NewTypeName(type, location), SyntaxFactory.IdentifierName(method)),
                SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(arguments,
                    Enumerable.Repeat(SyntaxFactory.Token(SyntaxKind.CommaToken).WithTrailingTrivia(SyntaxFactory.Space), arguments.Count - 1))));
        }

        // The arguments of a static call that returns its result, migrated; null when one is named, or passed by a reference
        // the method can write to
        private List<ExpressionSyntax> ValueArguments(InvocationExpressionSyntax invocation, IMethodSymbol method)
        {
            var arguments = invocation.ArgumentList.Arguments;
            if (arguments.Count != method.Parameters.Length || arguments.Any(x => x.NameColon is not null))
                return null;
            for (int i = 0; i < arguments.Count; i++)
            {
                var keyword = arguments[i].RefKindKeyword;
                var onlyRead = keyword.IsKind(SyntaxKind.None) || keyword.IsKind(SyntaxKind.InKeyword)
                    || (keyword.IsKind(SyntaxKind.RefKeyword) && method.Parameters[i].RefKind is RefKind.In or RefKind.RefReadOnlyParameter);
                if (!onlyRead)
                    return null;
            }
            return [.. arguments.Select(x => ((ExpressionSyntax)Visit(x.Expression)).WithoutTrivia())];
        }

        // Whether what a statement assigns to can be an out argument of this type: a variable, which reads the same after the arguments
        private bool CanBeOut(ExpressionSyntax target, ITypeSymbol type)
        {
            if (!IsRepeatable(target) || !SymbolEqualityComparer.Default.Equals(semanticModel.GetTypeInfo(target, cancellationToken).Type, type))
                return false;
            return SymbolOf(target) is not (IPropertySymbol { ReturnsByRef: false } or IEventSymbol);
        }

        // T.M(ref a, out b) → T.Method(a, out b), and x.M() → T.Method(x, out x)
        private ExpressionSyntax VoidToOutCall(InvocationExpressionSyntax invocation, IMethodSymbol method, string newMethod)
        {
            var arguments = invocation.ArgumentList.Arguments;
            if (arguments.Count != method.Parameters.Length || arguments.Any(x => x.NameColon is not null))
                return null;

            if (method.IsStatic)
            {
                if (method.Parameters is not [.., { RefKind: RefKind.Out }] || method.Parameters.Count(x => x.RefKind == RefKind.Out) != 1)
                    return null;
                var values = arguments.Take(arguments.Count - 1).Select(x => (ExpressionSyntax)Visit(x.Expression)).ToList();
                return OutCall(method.ContainingType, invocation, newMethod, values, (ExpressionSyntax)Visit(arguments[^1].Expression));
            }

            if (method.IsReadOnly || method.Parameters.Any(x => x.RefKind == RefKind.Out)
                || invocation.Expression is not MemberAccessExpressionSyntax { Expression: var receiver } access || !IsRepeatable(receiver))
                return null;
            var value = ((ExpressionSyntax)Visit(receiver)).WithoutTrivia();
            // p->M(); changes the value the pointer gives
            if (access.IsKind(SyntaxKind.PointerMemberAccessExpression))
                value = SyntaxFactory.PrefixUnaryExpression(SyntaxKind.PointerIndirectionExpression, value);
            return OutCall(method.ContainingType, invocation, newMethod, arguments.Select(x => (ExpressionSyntax)Visit(x.Expression)).Prepend(value), value);
        }

        // T.M(a) inside a larger expression → the expression the migration gives, with a in it and a new variable for the result
        private ExpressionSyntax ResultExpression(InvocationExpressionSyntax invocation, IMethodSymbol method, string template, string resultName)
        {
            if (!method.IsStatic)
                return null;
            var arguments = invocation.ArgumentList.Arguments;
            for (int i = 0; i < arguments.Count; i++)
                template = template.Replace("{" + i + "}", Placeholder(i));
            var expression = SyntaxFactory.ParseExpression(template.Replace("{result}", ResultPlaceholder));
            var placeholders = expression.DescendantNodes().OfType<IdentifierNameSyntax>().Where(x => x.Identifier.ValueText.StartsWith(PlaceholderPrefix, StringComparison.Ordinal)).ToList();
            if (placeholders.GroupBy(PlaceholderIndex).Any(x => x.Count() > 1 && !IsRepeatable(arguments[x.Key].Expression)) || ValueArguments(invocation, method) is not { } values)
                return null;

            // The expression names the new type: the document imports its namespace
            NewTypeName(method.ContainingType, invocation);
            expression = expression.ReplaceNodes(placeholders,
                (placeholder, _) => placeholder.Parent is ArgumentSyntax ? values[PlaceholderIndex(placeholder)] : AsOperand(values[PlaceholderIndex(placeholder)]));
            var result = SyntaxFactory.Identifier(UnusedName(resultName, invocation));
            expression = expression.ReplaceTokens(expression.DescendantTokens().Where(x => x.ValueText == ResultPlaceholder), (token, _) => result.WithTriviaFrom(token));

            var standsAlone = invocation.Parent is ArgumentSyntax or EqualsValueClauseSyntax or ReturnStatementSyntax or ArrowExpressionClauseSyntax or ParenthesizedExpressionSyntax or LambdaExpressionSyntax
                || (invocation.Parent is AssignmentExpressionSyntax assignment && assignment.Right == invocation);
            return standsAlone ? expression : SyntaxFactory.ParenthesizedExpression(expression);
        }

        private const string ResultPlaceholder = "__result";

        // T.M(ref a, ref b, out c); → c = T.M(a, b);
        private StatementSyntax OutToAssignment(ExpressionStatementSyntax statement, InvocationExpressionSyntax invocation, IMethodSymbol method)
        {
            if (method.Parameters is not [.., { RefKind: RefKind.Out } result] || method.Parameters.Count(x => x.RefKind == RefKind.Out) != 1)
                return null;
            var arguments = invocation.ArgumentList.Arguments;
            if (arguments.Count != method.Parameters.Length || arguments.Any(x => x.NameColon is not null))
                return null;

            var callRule = method.IsStatic ? CallExpression(method, arguments[0].Expression) : null;
            if (!method.IsStatic || !(callRule is not null || NewLibraryHas(method, method.Parameters[..^1].Select(x => x.Type), result.Type) || HasReturningTwin(method)))
                return null;

            var call = ((InvocationExpressionSyntax)VisitConvertedCall(invocation, method)).WithoutTrivia();
            var newArguments = call.ArgumentList.Arguments;
            var target = newArguments[^1].Expression;
            newArguments = SyntaxFactory.SeparatedList(newArguments.Take(newArguments.Count - 1).Select(x => x.WithRefKindKeyword(default).WithExpression(x.Expression.WithoutLeadingTrivia())),
                newArguments.GetSeparators().Take(Math.Max(newArguments.Count - 2, 0)));
            call = call.WithArgumentList(call.ArgumentList.WithArguments(newArguments));
            ExpressionSyntax value = call;
            if (callRule is not null)
                value = FromCallRule(callRule.Value, method, [.. newArguments.Select(x => x.Expression)], invocation);

            StatementSyntax rewritten;
            switch (target)
            {
                // out var c / out T c
                case DeclarationExpressionSyntax { Designation: SingleVariableDesignationSyntax variable } declaration:
                    // A local can only be declared by a statement of a block
                    if (statement.Parent is not (BlockSyntax or SwitchSectionSyntax or GlobalStatementSyntax))
                        return null;
                    rewritten = SyntaxFactory.LocalDeclarationStatement(SyntaxFactory.VariableDeclaration(
                        declaration.Type.WithoutTrivia().WithTrailingTrivia(SyntaxFactory.Space),
                        SyntaxFactory.SingletonSeparatedList(SyntaxFactory.VariableDeclarator(variable.Identifier.WithoutTrivia()).WithInitializer(Initializer(value)))));
                    break;

                // out _ / out var _
                case IdentifierNameSyntax { Identifier.ValueText: "_" } when semanticModel.GetSymbolInfo(arguments[^1].Expression, cancellationToken).Symbol is IDiscardSymbol:
                case DeclarationExpressionSyntax { Designation: DiscardDesignationSyntax }:
                    rewritten = SyntaxFactory.ExpressionStatement(Assignment(SyntaxFactory.IdentifierName("_"), value));
                    break;

                case DeclarationExpressionSyntax:
                    return null;

                default:
                    rewritten = SyntaxFactory.ExpressionStatement(Assignment(target.WithoutTrivia(), value));
                    break;
            }
            return rewritten.WithTriviaFrom(statement);
        }

        // Whether the old type has the same method with its result returned: the one a library that keeps one form has
        private bool HasReturningTwin(IMethodSymbol method)
        {
            if (!migration.TwinsOnly)
                return false;
            var values = method.Parameters[..^1];
            var result = method.Parameters[^1].Type;
            return method.ContainingType.GetMembers(method.Name).OfType<IMethodSymbol>().Any(x =>
                x.IsStatic && SymbolEqualityComparer.Default.Equals(x.ReturnType, result) && x.Parameters.All(p => p.RefKind == RefKind.None)
                && x.Parameters.Select(p => p.Type).SequenceEqual(values.Select(p => p.Type), SymbolEqualityComparer.Default));
        }

        // T.M(ref a), where M only reads a → T.M(a)
        public override SyntaxNode VisitArgument(ArgumentSyntax node)
        {
            var visited = (ArgumentSyntax)base.VisitArgument(node);
            if (!(node.RefKindKeyword.IsKind(SyntaxKind.RefKeyword) || node.RefKindKeyword.IsKind(SyntaxKind.InKeyword))
                || semanticModel.GetOperation(node, cancellationToken) is not IArgumentOperation { Parameter: { RefKind: RefKind.In or RefKind.RefReadOnlyParameter } parameter }
                || parameter.ContainingSymbol is not IMethodSymbol method || (method.ReducedFrom ?? method).ContainingType is not { } type || !HasNewForms(type))
                return visited;
            return visited.WithRefKindKeyword(default).WithLeadingTrivia(visited.GetLeadingTrivia());
        }

        // x.M(ref a); → a = the expression the migration gives for M, of x and a
        private StatementSyntax RefToAssignment(ExpressionStatementSyntax statement, InvocationExpressionSyntax invocation, IMethodSymbol method)
        {
            if (method.IsStatic || !migration.RefCalls.TryGetValue((FullName(method.ContainingType), method.Name), out var template)
                || invocation.Expression is not MemberAccessExpressionSyntax { Expression: var receiver } access || !access.IsKind(SyntaxKind.SimpleMemberAccessExpression)
                || invocation.ArgumentList.Arguments is not [{ NameColon: null, Expression: var variable }] || !IsRepeatable(variable))
                return null;

            // The expression names the new type: the document imports its namespace
            NewTypeName(method.ContainingType, invocation);
            var target = ((ExpressionSyntax)Visit(variable)).WithoutTrivia();
            var value = FromTemplate(template, [(ExpressionSyntax)Visit(receiver), target], invocation, "value", "variable");
            return SyntaxFactory.ExpressionStatement(Assignment(target, value)).WithTriviaFrom(statement);
        }

        // x.M(a); → x = the expression the migration gives for M, of x and a
        private StatementSyntax InPlaceToExpression(ExpressionStatementSyntax statement, InvocationExpressionSyntax invocation, IMethodSymbol method)
        {
            if (method.IsStatic || !migration.InPlaceExpressions.TryGetValue((FullName(method.ContainingType), method.Name), out var template)
                || invocation.Expression is not MemberAccessExpressionSyntax { Expression: var receiver } access || !access.IsKind(SyntaxKind.SimpleMemberAccessExpression)
                || !IsRepeatable(receiver) || invocation.ArgumentList.Arguments.Any(x => x.NameColon is not null || !x.RefKindKeyword.IsKind(SyntaxKind.None)))
                return null;

            // The expression names the new type and its extension members: the document imports their namespaces
            NewTypeName(method.ContainingType, invocation);
            ImportNamespaceOf(method.ContainingType, invocation);
            var target = ((ExpressionSyntax)Visit(receiver)).WithoutTrivia();
            List<ExpressionSyntax> values = [target, .. invocation.ArgumentList.Arguments.Select(x => (ExpressionSyntax)Visit(x.Expression))];
            var value = FromTemplate(template, values, invocation, [.. method.Parameters.Select(x => x.Name).Prepend("value")]);
            return SyntaxFactory.ExpressionStatement(Assignment(target, value)).WithTriviaFrom(statement);
        }

        // T.M(a, out b, out c); → (b, c) = the expression the migration gives for M, of a
        private StatementSyntax OutsToDeconstruction(ExpressionStatementSyntax statement, InvocationExpressionSyntax invocation, IMethodSymbol method)
        {
            var arguments = invocation.ArgumentList.Arguments;
            if (!method.IsStatic || arguments.Count != method.Parameters.Length || arguments.Any(x => x.NameColon is not null))
                return null;
            var typeName = FullName(method.ContainingType);
            foreach (var rule in migration.Deconstructions)
            {
                var count = rule.ParameterTypes.Length;
                if (rule.Type != typeName || rule.Method != method.Name || method.Parameters.Length < count + 2
                    || !method.Parameters.Take(count).Select(x => FullName(x.Type)).SequenceEqual(rule.ParameterTypes)
                    || method.Parameters.Skip(count).Any(x => x.RefKind != RefKind.Out))
                    continue;

                var results = arguments.Skip(count).Select(x => x.Expression).ToList();
                var declares = results.Any(x => x is DeclarationExpressionSyntax { Designation: SingleVariableDesignationSyntax });
                // A local can only be declared by a statement of a block
                if (declares && statement.Parent is not (BlockSyntax or SwitchSectionSyntax or GlobalStatementSyntax))
                    return null;

                NewTypeName(method.ContainingType, invocation);
                ImportNamespaceOf(method.ContainingType, invocation);
                var value = FromTemplate(rule.Expression, [.. arguments.Take(count).Select(x => (ExpressionSyntax)Visit(x.Expression))], invocation, [.. method.Parameters.Take(count).Select(x => x.Name)]);
                // out var a, out var b → var (a, b)
                var allVar = results.All(x => x is DeclarationExpressionSyntax { Type.IsVar: true, Designation: SingleVariableDesignationSyntax });
                var targets = results.Select(x => allVar
                    ? ((SingleVariableDesignationSyntax)((DeclarationExpressionSyntax)x).Designation).Identifier.ValueText
                    : ((ExpressionSyntax)Visit(x)).WithoutTrivia().ToFullString());
                var text = $"{(allVar ? "var " : "")}({string.Join(", ", targets)}) = {value.ToFullString()};";
                return SyntaxFactory.ParseStatement(text).WithTriviaFrom(statement);
            }
            return null;
        }

        // x.M(a); → x = T.M(x, a);
        private StatementSyntax InPlaceToAssignment(ExpressionStatementSyntax statement, InvocationExpressionSyntax invocation, IMethodSymbol method)
        {
            if (method.IsStatic || method.IsReadOnly || invocation.Expression is not MemberAccessExpressionSyntax { Expression: var receiver } access || !IsRepeatable(receiver))
                return null;
            if (invocation.ArgumentList.Arguments.Any(x => x.NameColon is not null))
                return null;

            // The static method that takes the value and returns the changed one
            var type = method.ContainingType;
            if (!NewLibraryHas(method, method.Parameters.Select(x => x.Type).Prepend(type), type))
                return null;

            var visitedInvocation = (InvocationExpressionSyntax)VisitConvertedCall(invocation, method);
            var visitedAccess = (MemberAccessExpressionSyntax)visitedInvocation.Expression;
            var visitedReceiver = visitedAccess.Expression.WithoutTrivia();
            // p->M(); changes the value the pointer gives
            if (access.IsKind(SyntaxKind.PointerMemberAccessExpression))
                visitedReceiver = SyntaxFactory.PrefixUnaryExpression(SyntaxKind.PointerIndirectionExpression, visitedReceiver);
            var arguments = visitedInvocation.ArgumentList.Arguments.Insert(0, SyntaxFactory.Argument(visitedReceiver));
            if (arguments.Count > 1)
                arguments = arguments.Replace(arguments[1], arguments[1].WithLeadingTrivia(SyntaxFactory.Space));
            var call = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, NewTypeName(type, access), visitedAccess.Name.WithoutTrivia()),
                visitedInvocation.ArgumentList.WithArguments(arguments).WithoutTrivia());
            return SyntaxFactory.ExpressionStatement(Assignment(visitedReceiver, call)).WithTriviaFrom(statement);
        }

        // The call with its parts migrated, for a call that takes the form of a static method by value of the new library
        private SyntaxNode VisitConvertedCall(InvocationExpressionSyntax invocation, IMethodSymbol method)
        {
            if (invocation.Expression is MemberAccessExpressionSyntax { Name: var name })
                convertedCalls.Add(name);
            ImportExtensionMember(MovedTo(method, out _) ?? method.ContainingType, NewMemberName(method) ?? method.Name, invocation);
            return base.VisitInvocationExpression(invocation);
        }

        // An expression that reads the same when written twice: names, fields, elements at such indices, and their arithmetic
        private static bool IsRepeatable(ExpressionSyntax expression) => expression switch
        {
            IdentifierNameSyntax or ThisExpressionSyntax or LiteralExpressionSyntax => true,
            MemberAccessExpressionSyntax access => IsRepeatable(access.Expression),
            ElementAccessExpressionSyntax element => IsRepeatable(element.Expression) && element.ArgumentList.Arguments.All(x => IsRepeatable(x.Expression)),
            ParenthesizedExpressionSyntax parenthesized => IsRepeatable(parenthesized.Expression),
            BinaryExpressionSyntax binary => IsRepeatable(binary.Left) && IsRepeatable(binary.Right),
            _ => false,
        };

        private static EqualsValueClauseSyntax Initializer(ExpressionSyntax value)
            => SyntaxFactory.EqualsValueClause(SyntaxFactory.Token(SyntaxKind.EqualsToken).WithLeadingTrivia(SyntaxFactory.Space).WithTrailingTrivia(SyntaxFactory.Space), value);

        private static AssignmentExpressionSyntax Assignment(ExpressionSyntax target, ExpressionSyntax value)
            => SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, target,
                SyntaxFactory.Token(SyntaxKind.EqualsToken).WithLeadingTrivia(SyntaxFactory.Space).WithTrailingTrivia(SyntaxFactory.Space), value);

        private string OperatorMethod(SyntaxNode node)
        {
            if (SymbolOf(node) is not IMethodSymbol { MethodKind: MethodKind.UserDefinedOperator, Parameters: [var left, var right] } method
                || !IsMigrated(method.ContainingType) || !SymbolEqualityComparer.Default.Equals(left.Type, right.Type))
                return null;
            var typeName = FullName(method.ContainingType);
            return migration.Operators.FirstOrDefault(x => x.Type == typeName && x.Operator == method.Name).Method;
        }

        private InvocationExpressionSyntax StaticCall(INamedTypeSymbol oldType, SyntaxNode location, string method, params ExpressionSyntax[] arguments)
        {
            var list = SyntaxFactory.SeparatedList(arguments.Select(x => SyntaxFactory.Argument(x.WithoutTrivia())),
                Enumerable.Repeat(SyntaxFactory.Token(SyntaxKind.CommaToken).WithTrailingTrivia(SyntaxFactory.Space), arguments.Length - 1));
            ImportExtensionMember(oldType, method, location);
            return SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, NewTypeName(oldType, location), SyntaxFactory.IdentifierName(method)),
                SyntaxFactory.ArgumentList(list));
        }

        // Only a primary expression takes a member access without parentheses
        private static ExpressionSyntax AsReceiver(ExpressionSyntax expression)
        {
            expression = expression.WithoutTrivia();
            return expression is IdentifierNameSyntax or MemberAccessExpressionSyntax or InvocationExpressionSyntax or ElementAccessExpressionSyntax
                or ParenthesizedExpressionSyntax or ThisExpressionSyntax or ObjectCreationExpressionSyntax
                ? expression
                : SyntaxFactory.ParenthesizedExpression(expression);
        }

        // a * b → T.Method(a, b), a == b → a.Method(b)
        public override SyntaxNode VisitBinaryExpression(BinaryExpressionSyntax node)
        {
            // Between two values: with a nullable value or null, the comparison stays the one of the language
            if (SymbolOf(node) is IMethodSymbol { MethodKind: MethodKind.UserDefinedOperator, Parameters: [var leftParameter, var rightParameter] } @operator
                && IsMigrated(@operator.ContainingType) && SymbolEqualityComparer.Default.Equals(leftParameter.Type, rightParameter.Type)
                && IsValue(node.Left) && IsValue(node.Right))
            {
                var operatorType = FullName(@operator.ContainingType);
                if (migration.OperatorExpressions.TryGetValue((operatorType, @operator.Name), out var template))
                {
                    NewTypeName(@operator.ContainingType, node);
                    var expression = FromTemplate(template, [(ExpressionSyntax)Visit(node.Left), (ExpressionSyntax)Visit(node.Right)], node, leftParameter.Name, rightParameter.Name);
                    return InPlaceOf(node, expression).WithTriviaFrom(node);
                }

                var instance = migration.InstanceOperators.FirstOrDefault(x => x.Type == operatorType && x.Operator == @operator.Name);
                if (instance.Method is not null)
                {
                    ImportExtensionMember(@operator.ContainingType, instance.Method, node);
                    ExpressionSyntax call = SyntaxFactory.InvocationExpression(
                        SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, AsReceiver((ExpressionSyntax)Visit(node.Left)), SyntaxFactory.IdentifierName(instance.Method)),
                        SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(((ExpressionSyntax)Visit(node.Right)).WithoutTrivia()))));
                    if (instance.Negate)
                        call = SyntaxFactory.PrefixUnaryExpression(SyntaxKind.LogicalNotExpression, call);
                    return call.WithTriviaFrom(node);
                }
            }

            if (OperatorMethod(node) is not { } method)
            {
                ImportOperator(node);
                return base.VisitBinaryExpression(node);
            }
            var type = ((IMethodSymbol)SymbolOf(node)).ContainingType;
            return StaticCall(type, node, method, (ExpressionSyntax)Visit(node.Left), (ExpressionSyntax)Visit(node.Right)).WithTriviaFrom(node);

            bool IsValue(ExpressionSyntax operand) => IsMigrated(semanticModel.GetTypeInfo(operand, cancellationToken).Type);
        }

        // An operator the new type does not have is an extension member too
        private void ImportOperator(SyntaxNode node)
        {
            if (SymbolOf(node) is IMethodSymbol { MethodKind: MethodKind.UserDefinedOperator } @operator && IsMigrated(@operator.ContainingType))
                ImportExtensionMember(@operator.ContainingType, @operator.Name, node, @operator);
        }

        // +a → the expression the migration gives for the operator
        public override SyntaxNode VisitPrefixUnaryExpression(PrefixUnaryExpressionSyntax node)
        {
            if (SymbolOf(node) is IMethodSymbol { MethodKind: MethodKind.UserDefinedOperator, Parameters: [var parameter] } @operator && IsMigrated(@operator.ContainingType)
                && migration.OperatorExpressions.TryGetValue((FullName(@operator.ContainingType), @operator.Name), out var template))
            {
                return InPlaceOf(node, FromTemplate(template, [(ExpressionSyntax)Visit(node.Operand)], node, parameter.Name)).WithTriviaFrom(node);
            }
            ImportOperator(node);
            return base.VisitPrefixUnaryExpression(node);
        }

        // a *= b → a = T.Method(a, b)
        public override SyntaxNode VisitAssignmentExpression(AssignmentExpressionSyntax node)
        {
            // a /= b → a = the expression the migration gives for the operator
            if (!node.IsKind(SyntaxKind.SimpleAssignmentExpression) && IsRepeatable(node.Left)
                && SymbolOf(node) is IMethodSymbol { MethodKind: MethodKind.UserDefinedOperator, Parameters: [var leftParameter, var rightParameter] } compound
                && IsMigrated(compound.ContainingType) && SymbolEqualityComparer.Default.Equals(leftParameter.Type, rightParameter.Type)
                && migration.OperatorExpressions.TryGetValue((FullName(compound.ContainingType), compound.Name), out var template))
            {
                NewTypeName(compound.ContainingType, node);
                var target = (ExpressionSyntax)Visit(node.Left);
                return Assignment(target.WithoutTrivia(), FromTemplate(template, [target, (ExpressionSyntax)Visit(node.Right)], node, leftParameter.Name, rightParameter.Name)).WithTriviaFrom(node);
            }

            if (node.IsKind(SyntaxKind.SimpleAssignmentExpression) || !IsRepeatable(node.Left) || OperatorMethod(node) is not { } method)
            {
                ImportOperator(node);
                return base.VisitAssignmentExpression(node);
            }
            var type = ((IMethodSymbol)SymbolOf(node)).ContainingType;
            var left = (ExpressionSyntax)Visit(node.Left);
            return Assignment(left.WithoutTrivia(), StaticCall(type, node, method, left, (ExpressionSyntax)Visit(node.Right))).WithTriviaFrom(node);
        }

        private string ConstructorMethod(SyntaxNode creation, out INamedTypeSymbol type)
        {
            type = null;
            if (SymbolOf(creation) is not IMethodSymbol { MethodKind: MethodKind.Constructor } constructor || !IsMigrated(constructor.ContainingType))
                return null;
            type = constructor.ContainingType;
            var typeName = FullName(type);
            var parameters = constructor.Parameters.Select(x => FullName(x.Type)).ToArray();
            var method = migration.Constructors.FirstOrDefault(x => x.Type == typeName && x.ParameterTypes.SequenceEqual(parameters)).Method;
            if (method is not null)
                ImportExtensionMember(type, method, creation, constructor);
            return method;
        }

        // new T(a, b) → the expression the migration gives for this constructor, with a and b in it
        private ExpressionSyntax ConstructorExpression(SyntaxNode creation, ArgumentListSyntax argumentList)
        {
            if (SymbolOf(creation) is not IMethodSymbol { MethodKind: MethodKind.Constructor } constructor || !IsMigrated(constructor.ContainingType))
                return null;
            var typeName = FullName(constructor.ContainingType);
            var parameters = constructor.Parameters.Select(x => FullName(x.Type)).ToArray();
            var template = migration.ConstructorExpressions.FirstOrDefault(x => x.Type == typeName && x.ParameterTypes.SequenceEqual(parameters)).Expression;
            if (template is null || argumentList.Arguments.Count != parameters.Length || argumentList.Arguments.Any(x => x.NameColon is not null))
                return null;

            // The expression names the new type: the document imports its namespace
            NewTypeName(constructor.ContainingType, creation);
            return FromTemplate(template, [.. argumentList.Arguments.Select(x => (ExpressionSyntax)Visit(x.Expression))], creation, [.. constructor.Parameters.Select(x => x.Name)]);
        }

        private const string PlaceholderPrefix = "__argument";

        private static string Placeholder(int index) => PlaceholderPrefix + index.ToString(CultureInfo.InvariantCulture);

        private static int PlaceholderIndex(IdentifierNameSyntax placeholder) => int.Parse(placeholder.Identifier.ValueText[PlaceholderPrefix.Length..], CultureInfo.InvariantCulture);

        private static ExpressionSyntax AsOperand(ExpressionSyntax value)
            => value is IdentifierNameSyntax or MemberAccessExpressionSyntax or InvocationExpressionSyntax or ElementAccessExpressionSyntax or LiteralExpressionSyntax
                or ParenthesizedExpressionSyntax or ObjectCreationExpressionSyntax or ThisExpressionSyntax
                ? value
                : SyntaxFactory.ParenthesizedExpression(value);

        // The names of each member: the ones it had, and the ones given to new variables, which the semantic model does not see
        private readonly Dictionary<SyntaxNode, HashSet<string>> memberNames = [];

        // A name that nothing has in the member around this place: the variable of an expression is seen in its whole block
        private string UnusedName(string name, SyntaxNode location)
        {
            var member = location.Ancestors().FirstOrDefault(x => x is BaseMethodDeclarationSyntax or BasePropertyDeclarationSyntax or BaseFieldDeclarationSyntax)
                ?? location.SyntaxTree.GetRoot(cancellationToken);
            if (!memberNames.TryGetValue(member, out var names))
                memberNames[member] = names = [.. member.DescendantTokens().Where(x => x.IsKind(SyntaxKind.IdentifierToken)).Select(x => x.ValueText)];
            var candidate = name;
            for (int i = 2; names.Contains(candidate) || !semanticModel.LookupSymbols(location.SpanStart, name: candidate).IsEmpty; i++)
                candidate = name + i.ToString(CultureInfo.InvariantCulture);
            names.Add(candidate);
            return candidate;
        }

        // new T { P = value } → the expression the migration gives for the setter of P, when it is made of the value alone
        private ExpressionSyntax InitializerExpression(BaseObjectCreationExpressionSyntax creation)
        {
            if (creation.ArgumentList is { Arguments.Count: > 0 }
                || creation.Initializer is not { RawKind: (int)SyntaxKind.ObjectInitializerExpression, Expressions: [AssignmentExpressionSyntax { Left: IdentifierNameSyntax name, Right: var value }] }
                || SymbolOf(name) is not IPropertySymbol { ContainingType: { } type } property || !IsReplaced(type)
                || !migration.SetterExpressions.TryGetValue((FullName(type), property.Name), out var rule) || rule.Expression.Contains("{0}", StringComparison.Ordinal))
                return null;
            if (rule.Import is not null && !IsImported(rule.Import, creation))
                NamespacesToImport.Add(rule.Import);
            return InPlaceOf(creation, FromTemplate(rule.Expression, [name, (ExpressionSyntax)Visit(value)], creation, "target", "value")).WithTriviaFrom(creation);
        }

        // new T(a) → T.Method(a)
        public override SyntaxNode VisitObjectCreationExpression(ObjectCreationExpressionSyntax node)
        {
            if (InitializerExpression(node) is { } initialized)
                return initialized;
            if (node.Initializer is null && node.ArgumentList is not null && ConstructorExpression(node, node.ArgumentList) is { } expression)
                return expression.WithTriviaFrom(node);
            if (node.Initializer is not null || node.ArgumentList is null || ConstructorMethod(node, out var type) is not { } method)
                return base.VisitObjectCreationExpression(node);
            var arguments = (ArgumentListSyntax)Visit(node.ArgumentList);
            return SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, NewTypeName(type, node), SyntaxFactory.IdentifierName(method)),
                arguments.WithoutTrivia()).WithTriviaFrom(node);
        }

        // new(a) → T.Method(a)
        public override SyntaxNode VisitImplicitObjectCreationExpression(ImplicitObjectCreationExpressionSyntax node)
        {
            if (InitializerExpression(node) is { } initialized)
                return initialized;
            if (node.Initializer is null && ConstructorExpression(node, node.ArgumentList) is { } expression)
                return expression.WithTriviaFrom(node);
            if (node.Initializer is not null || ConstructorMethod(node, out var type) is not { } method)
                return base.VisitImplicitObjectCreationExpression(node);
            var arguments = (ArgumentListSyntax)Visit(node.ArgumentList);
            return SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, NewTypeName(type, node), SyntaxFactory.IdentifierName(method)),
                arguments.WithoutTrivia()).WithTriviaFrom(node);
        }

        // $"{x:F2}" → $"{x.Method("F2", null)}", for a type that no longer formats itself
        public override SyntaxNode VisitInterpolation(InterpolationSyntax node)
        {
            var visited = (InterpolationSyntax)base.VisitInterpolation(node);
            if (node.FormatClause is not { } format || semanticModel.GetTypeInfo(node.Expression, cancellationToken).Type is not INamedTypeSymbol type
                || !IsReplaced(type) || !migration.FormatMethods.TryGetValue(FullName(type), out var method))
                return visited;

            ImportNamespaceOf(type, node);
            var arguments = SyntaxFactory.SeparatedList<ArgumentSyntax>(
                [SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(format.FormatStringToken.ValueText))),
                    SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression))],
                [SyntaxFactory.Token(SyntaxKind.CommaToken).WithTrailingTrivia(SyntaxFactory.Space)]);
            var call = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, AsReceiver(visited.Expression), SyntaxFactory.IdentifierName(method)),
                SyntaxFactory.ArgumentList(arguments));
            return visited.WithExpression(call.WithTriviaFrom(visited.Expression)).WithFormatClause(null);
        }

        // (To)x → x.Method()
        public override SyntaxNode VisitCastExpression(CastExpressionSyntax node)
        {
            if (SymbolOf(node) is IMethodSymbol { MethodKind: MethodKind.Conversion, Parameters: [var parameter] } conversion
                && IsMigrated(parameter.Type) && IsMigrated(conversion.ReturnType))
            {
                var from = FullName(parameter.Type);
                var to = FullName(conversion.ReturnType);
                if (migration.Casts.FirstOrDefault(x => x.From == from && x.To == to).Method is { } method)
                {
                    ImportExtensionMember((INamedTypeSymbol)parameter.Type, method, node);
                    return SyntaxFactory.InvocationExpression(
                        SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, AsReceiver((ExpressionSyntax)Visit(node.Expression)), SyntaxFactory.IdentifierName(method)))
                        .WithTriviaFrom(node);
                }
            }
            return base.VisitCastExpression(node);
        }
    }
}
