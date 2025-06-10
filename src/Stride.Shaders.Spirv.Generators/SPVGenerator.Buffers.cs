using System.Text;

namespace Stride.Shaders.Spirv.Generators;

public partial class SPVGenerator
{
    public static void CreateOperation(InstructionData op, StringBuilder code, Dictionary<string, OpKind> operandKinds)
    {
        var opname = op.OpName;
        if (opname == "OpConstant")
        {
            code.AppendLine(op.Documentation);
            code
                .AppendLine("public static Instruction AddOpConstant<TValue>(this SpirvBuffer buffer, IdResult resultId, IdResultType? resultType, TValue value) where TValue : struct, ILiteralNumber")
                .AppendLine("{")
                    .AppendLine("var wordLength = 1 + buffer.GetWordLength(resultType) + buffer.GetWordLength(resultId) + value.WordCount;")
                    .AppendLine("return buffer.Add([wordLength << 16 | (int)SDSLOp.OpConstant, ..resultType.AsSpirvSpan(), resultId, ..value.AsSpirvSpan()]);")

                .AppendLine("}");


            code.AppendLine(op.Documentation);
            code
                .AppendLine("public static Instruction InsertOpConstant<TValue>(this SpirvBuffer buffer, int position, IdResult resultId, IdResultType? resultType, TValue value) where TValue : struct, ILiteralNumber")
                .AppendLine("{")
                    .AppendLine("var wordLength = 1 + buffer.GetWordLength(resultType) + buffer.GetWordLength(resultId) + value.WordCount;")
                    .AppendLine("return buffer.Insert(position, [wordLength << 16 | (int)SDSLOp.OpConstant, ..resultType.AsSpirvSpan(), resultId, ..value.AsSpirvSpan()]);")

                .AppendLine("}");

        }
        else if (opname == "OpSpecConstant")
        {
            code
                .AppendLine("public static Instruction AddOpSpecConstant<TValue>(this SpirvBuffer buffer, IdResult resultId, IdResultType? resultType, TValue value) where TValue : struct, ILiteralNumber")
                .AppendLine("{")

                    .AppendLine("var wordLength = 1 + buffer.GetWordLength(resultType) + buffer.GetWordLength(resultId) + value.WordCount;")
                    .AppendLine("return buffer.Add([wordLength << 16 | (int)SDSLOp.OpSpecConstant, ..resultType.AsSpirvSpan(), resultId, ..value.AsSpirvSpan()]);")
                .AppendLine("}");
            code.AppendLine(op.Documentation);
            code
                .AppendLine("public static Instruction InsertOpSpecConstant<TValue>(this SpirvBuffer buffer, int position, IdResult resultId, IdResultType? resultType, TValue value) where TValue : struct, ILiteralNumber")
                .AppendLine("{")

                    .AppendLine("var wordLength = 1 + buffer.GetWordLength(resultType) + buffer.GetWordLength(resultId) + value.WordCount;")
                    .AppendLine("return buffer.Insert(position, [wordLength << 16 | (int)SDSLOp.OpSpecConstant, ..resultType.AsSpirvSpan(), resultId, ..value.AsSpirvSpan()]);")
                .AppendLine("}");
        }
        else if (opname!.StartsWith("OpDecorate"))
        {
            code
                .Append("public static Instruction Add").Append(opname).Append("(this SpirvBuffer buffer, IdRef target, Decoration decoration, int? additional1 = null, int? additional2 = null, string? additionalString = null)")
                .AppendLine("{")

                    .AppendLine("var wordLength = 1 + buffer.GetWordLength(target) + buffer.GetWordLength(decoration) + buffer.GetWordLength(additional1) + buffer.GetWordLength(additional2) + buffer.GetWordLength(additionalString);")
                    .AppendLine("return buffer.Add([wordLength << 16 | (int)SDSLOp.OpDecorate, target, ..decoration.AsSpirvSpan(), ..additional1.AsSpirvSpan(), ..additional2.AsSpirvSpan(), ..additionalString.AsSpirvSpan()]);")

                .AppendLine("}");
            code.AppendLine(op.Documentation);
            code
                .Append("public static Instruction Insert").Append(opname).Append("(this SpirvBuffer buffer, int position, IdRef target, Decoration decoration, int? additional1 = null, int? additional2 = null, string? additionalString = null)")
                .AppendLine("{")

                    .AppendLine("var wordLength = 1 + buffer.GetWordLength(target) + buffer.GetWordLength(decoration) + buffer.GetWordLength(additional1) + buffer.GetWordLength(additional2) + buffer.GetWordLength(additionalString);")
                    .AppendLine("return buffer.Insert(position, [wordLength << 16 | (int)SDSLOp.OpDecorate, target, ..decoration.AsSpirvSpan(), ..additional1.AsSpirvSpan(), ..additional2.AsSpirvSpan(), ..additionalString.AsSpirvSpan()]);")

                .AppendLine("}");
        }
        else if (opname.StartsWith("OpMemberDecorate"))
        {
            code
                .Append("public static Instruction Add").Append(opname).Append("(this SpirvBuffer buffer, IdRef structureType, LiteralInteger member, Decoration decoration, int? additional1 = null, int? additional2 = null, string? additionalString = null)")
                .AppendLine("{")

                    .AppendLine("var wordLength = 1 + buffer.GetWordLength(structureType) + buffer.GetWordLength(member) + buffer.GetWordLength(decoration) + buffer.GetWordLength(additional1) + buffer.GetWordLength(additional2) + buffer.GetWordLength(additionalString);")
                    .AppendLine("return buffer.Add([wordLength << 16 | (int)SDSLOp.OpMemberDecorate, ..structureType.AsSpirvSpan(), ..member.AsSpirvSpan(), ..decoration.AsSpirvSpan(), ..additional1.AsSpirvSpan(), ..additional2.AsSpirvSpan(), ..additionalString.AsSpirvSpan()]);")

                .AppendLine("}");
            code.AppendLine(op.Documentation);
            code
                .Append("public static Instruction Insert").Append(opname).Append("(this SpirvBuffer buffer, int position, IdRef structureType, LiteralInteger member, Decoration decoration, int? additional1 = null, int? additional2 = null, string? additionalString = null)")
                .AppendLine("{")

                    .AppendLine("var wordLength = 1 + buffer.GetWordLength(structureType) + buffer.GetWordLength(member) + buffer.GetWordLength(decoration) + buffer.GetWordLength(additional1) + buffer.GetWordLength(additional2) + buffer.GetWordLength(additionalString);")
                    .AppendLine("return buffer.Insert(position, [wordLength << 16 | (int)SDSLOp.OpMemberDecorate, ..structureType.AsSpirvSpan(), ..member.AsSpirvSpan(), ..decoration.AsSpirvSpan(), ..additional1.AsSpirvSpan(), ..additional2.AsSpirvSpan(), ..additionalString.AsSpirvSpan()]);")

                .AppendLine("}");
        }

        else if (op.Operands is EquatableArray<OperandData> operands && operands.Count > 0)
        {
            var parameters = ConvertOperandsToParameters(op, operandKinds);
            var parameterNames = ConvertOperandsToParameterNames(op, operandKinds);
            var hasResultId = parameterNames.Contains("resultId") && opname != "OpExtInst";
            if (hasResultId)
            {
                parameters.Remove(parameters.First(x => x.Contains("resultId")));
            }
            var paramsParameters = parameters.Where(x => x.StartsWith("Span"));
            var nullableParameters = parameters.Where(x => x.Contains("?"));
            var normalParameters = parameters.Where(x => !x.Contains("?") && !x.StartsWith("Span"));

            code
                .Append("public static Instruction Add")
                .Append(opname)
                .Append("(this SpirvBuffer buffer")
                .Append(hasResultId ? ", IdResult resultId" : "")
                .Append(normalParameters.Count() + nullableParameters.Count() + paramsParameters.Count() == 0 ? "" : ", ")
                .Append(string.Join(", ", normalParameters))
                .Append(nullableParameters.Count() == 0 ? "" : (normalParameters.Count() > 0 ? ", " : "") + string.Join(", ", nullableParameters))
                .Append(paramsParameters.Count() == 0 ? "" : (normalParameters.Count() + nullableParameters.Count() > 0 ? ", " : "") + paramsParameters.First())
                .AppendLine(")")
                .AppendLine("{")
                ;
            code.Append("var wordLength = 1").Append(parameterNames.Any() ? " + " : "").Append(string.Join(" + ", parameterNames.Select(x => $"buffer.GetWordLength({x})"))).AppendLine(";");
            code
                .AppendLine($"return buffer.Add([wordLength << 16 | (int)SDSLOp.{opname}, {string.Join(", ", parameterNames.Select(x => $"..{x}.AsSpirvSpan()"))}]);")
                .AppendLine("}");



            code.AppendLine(op.Documentation);
            code
                .Append("public static Instruction Insert")
                .Append(opname)
                .Append("(this SpirvBuffer buffer, int position")
                .Append(hasResultId ? ", IdResult resultId" : "")
                .Append(normalParameters.Count() + nullableParameters.Count() + paramsParameters.Count() == 0 ? "" : ", ")
                .Append(string.Join(", ", normalParameters))
                .Append(nullableParameters.Count() == 0 ? "" : (normalParameters.Count() > 0 ? ", " : "") + string.Join(", ", nullableParameters))
                .Append(paramsParameters.Count() == 0 ? "" : (normalParameters.Count() + nullableParameters.Count() > 0 ? ", " : "") + paramsParameters.First())
                .AppendLine(")")
                .AppendLine("{")
                ;
            code.Append("var wordLength = 1").Append(parameterNames.Any() ? " + " : "").Append(string.Join(" + ", parameterNames.Select(x => $"buffer.GetWordLength({x})"))).AppendLine(";");
            code
                .AppendLine($"return buffer.Insert(position, [wordLength << 16 | (int)SDSLOp.{opname}, {string.Join(", ", parameterNames.Select(x => $"..{x}.AsSpirvSpan()"))}]);")
                .AppendLine("}");
        }
        else
        {
            code
                .Append("public static Instruction Add")
                .Append(opname)
                .AppendLine("(this SpirvBuffer buffer)")
                .AppendLine("{")

                    .AppendLine($"return buffer.Add([1 << 16 | (int)SDSLOp.{opname}]);")

                .AppendLine("}");
            code.AppendLine(op.Documentation);
            code
                .Append("public static Instruction Insert")
                .Append(opname)
                .AppendLine("(this SpirvBuffer buffer, int position)")
                .AppendLine("{")
                    .AppendLine($"return buffer.Insert(position, [1 << 16 | (int)SDSLOp.{opname}]);")
                .AppendLine("}");
        }
    }

    public static void CreateGlslOperation(InstructionData op, StringBuilder code, Dictionary<string, OpKind> operandKinds)
    {
        var opname = op.OpName;
        var opcode = op.OpCode;

        if (op.Operands is not null)
        {
            var parameters = ConvertOperandsToParameters(op, operandKinds);
            parameters.Add("int set");

            var parameterNames = ConvertOperandsToParameterNames(op, operandKinds);
            parameterNames.Add("set");

            var hasResultId = parameterNames.Contains("resultId");

            if (hasResultId)
            {
                parameters.Remove(parameters.First(x => x.Contains("resultId")));
            }

            var paramsParameters = parameters.Where(x => x.Contains("Span"));
            var nullableParameters = parameters.Where(x => x.Contains("?"));
            var normalParameters = parameters.Where(x => !x.Contains("?") && !x.Contains("Span"));
            var other = parameterNames.Where(x => x != "resultType" && x != "resultId" && x != "set");

            // var cells = glslDoc!.QuerySelectorAll($"p.tableblock:has(strong:contains(\"{opname}\"))");
            // var comment = AddDocComment(cells);
            code.AppendLine(op.Documentation);
            code
                .Append("public static Instruction AddGLSL")
                .Append(opname)
                .Append("(this SpirvBuffer buffer, IdResultType resultType, int resultId, ")
                .Append(string.Join(", ", normalParameters))
                .Append(nullableParameters.Count() == 0 ? "" : (normalParameters.Count() > 0 ? ", " : "") + string.Join(", ", nullableParameters))
                .Append(paramsParameters.Count() == 0 ? "" : (normalParameters.Count() + nullableParameters.Count() > 0 ? ", " : "") + paramsParameters.First())
                .AppendLine(")")
                .AppendLine("{")
                    .Append("Span<IdRef> refs = [").Append(string.Join(", ", other)).AppendLine("];")
                    .Append("return buffer.AddOpExtInst(")
                        .Append("set, ")
                        .Append(opcode)
                        .Append(", resultId, resultType ")
                        .AppendLine(", refs);")
                .AppendLine("}");

            code.AppendLine(op.Documentation);
            code
                .Append("public static Instruction InsertGLSL")
                .Append(opname)
                .Append("(this SpirvBuffer buffer, int position, IdResultType resultType, int resultId, ")
                .Append(string.Join(", ", normalParameters))
                .Append(nullableParameters.Count() == 0 ? "" : (normalParameters.Count() > 0 ? ", " : "") + string.Join(", ", nullableParameters))
                .Append(paramsParameters.Count() == 0 ? "" : (normalParameters.Count() + nullableParameters.Count() > 0 ? ", " : "") + paramsParameters.First())
                .AppendLine(")")
                .AppendLine("{")
                    .Append("Span<IdRef> refs = [").Append(string.Join(", ", other)).AppendLine("];")
                    .Append("return buffer.InsertOpExtInst(position, ")
                        .Append("set, ")
                        .Append(opcode)
                        .Append(", resultId, resultType ")
                        .AppendLine(", refs);")
                .AppendLine("}");
        }
    }
}