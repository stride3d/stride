// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.Components.FixReferences;

/// <summary>
/// Base view model for the fix reference component. This component allows to replace references to objects that
/// are being deleted by references to replacement objects chosen by the user. This view model presents the objects
/// to fix sequentially, and information on the progress of the reference-fixing. Therefore, it works with an
/// enumerator internally and presents the <see cref="CurrentObjectToReplace"/>.
/// </summary>
/// <remarks>
/// The WPF view model closed its dialog when the last object was handled; this one raises <see cref="Completed"/> instead,
/// so that the view that shows it closes.
/// </remarks>
/// <typeparam name="T">The type of objects that are being deleted.</typeparam>
public abstract class FixReferencesViewModel<T> : DispatcherViewModel where T : ViewModelBase
{
    private readonly record struct ReferenceTarget(T ReferencedObject, object ReferencedMember);

    private readonly List<ReferenceReplacementViewModel<T>> referencesToFix = [];
    private readonly List<ReferenceReplacementViewModel<T>> referencesToClear = [];
    private readonly Dictionary<ReferenceTarget, List<T>> referencersMap = [];

    private T? currentObjectToReplace;
    private IEnumerator<KeyValuePair<ReferenceTarget, List<T>>>? currentObjectToReplaceEnumerator;
    private int fixedObjectsCount;
    private bool useSingleReplace = true;
    private T? singleReplacementObject;

    /// <summary>
    /// Initializes a new instance of the <see cref="FixReferencesViewModel{T}"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider for this view model.</param>
    protected FixReferencesViewModel(IViewModelServiceProvider serviceProvider)
        : base(serviceProvider)
    {
        CurrentReplacements = [];
        PickupSingleReplacementObjectCommand = new AnonymousTaskCommand(serviceProvider, PickupSingleReplacementObject);
        FixReferencesCommand = new AnonymousCommand(serviceProvider, () => FixReferences());
        ClearReferencesCommand = new AnonymousCommand(serviceProvider, () => ClearReferences());
        ClearAllReferencesCommand = new AnonymousCommand(serviceProvider, ClearAllReferences);
    }

    /// <summary>
    /// Raised when every object to fix has been handled: the fixes can be applied with <see cref="ProcessFixes"/>.
    /// </summary>
    public event EventHandler? Completed;

    /// <summary>
    /// Gets whether every object to fix has been handled.
    /// </summary>
    public bool IsCompleted { get; private set; }

    /// <summary>
    /// Gets the total number of objects to fix.
    /// </summary>
    public int ObjectsToFixCount => referencersMap.Count;

    /// <summary>
    /// Gets the number of objects that have been already fixed by user.
    /// </summary>
    /// <remarks>This value represents the progress of the reference-fixing operation.</remarks>
    public int FixedObjectsCount { get => fixedObjectsCount; set => SetValue(ref fixedObjectsCount, value, nameof(FixedObjectsCount), nameof(FixedObjectsCounter)); }

    /// <summary>
    /// Gets a formatted string representing the number of fixed objects, over the total number of objects to fix.
    /// </summary>
    /// <remarks>This string represents the progress of the reference-fixing operation.</remarks>
    public string FixedObjectsCounter => $"{FixedObjectsCount}/{ObjectsToFixCount}";

    /// <summary>
    /// Gets the current object to replace, for which the user must decide the replacements to do.
    /// </summary>
    public T? CurrentObjectToReplace { get => currentObjectToReplace; private set => SetValue(ref currentObjectToReplace, value); }

    /// <summary>
    /// Gets or sets whether to use the same replacement object for all the objects to fix.
    /// </summary>
    public bool UseSingleReplace { get => useSingleReplace; set => SetValue(ref useSingleReplace, value, UpdateCommands); }

    /// <summary>
    /// Gets or sets the replacement to use when <see cref="UseSingleReplace"/> is <c>true</c>.
    /// </summary>
    public T? SingleReplacementObject { get => singleReplacementObject; set { SetValue(ref singleReplacementObject, value); UpdateReplacements(); } }

    /// <summary>
    /// Gets the current replacements to resolve by the user, corresponding to the <see cref="CurrentObjectToReplace"/>.
    /// </summary>
    public ObservableList<ReferenceReplacementViewModel<T>> CurrentReplacements { get; }

    /// <summary>
    /// Gets a command that will display a pickup dialog and use the selected asset as replacement of the deleted
    /// asset for all references that need to be fixed
    /// </summary>
    public ICommandBase PickupSingleReplacementObjectCommand { get; }

    /// <summary>
    /// Gets a command that indicates to fix the references for the <see cref="CurrentObjectToReplace"/> and
    /// move the next object to fix (or complete the process if the current object is the last one).
    /// </summary>
    public ICommandBase FixReferencesCommand { get; }

    /// <summary>
    /// Gets a command that indicates to clear the references for the <see cref="CurrentObjectToReplace"/> and
    /// move the next object to fix (or complete the process if the current object is the last one).
    /// </summary>
    public ICommandBase ClearReferencesCommand { get; }

    /// <summary>
    /// Gets a command that indicates to clear the references for the <see cref="CurrentObjectToReplace"/> and
    /// the following object to fix, and complete the process if the current object is the last one.
    /// </summary>
    public ICommandBase ClearAllReferencesCommand { get; }

    /// <summary>
    /// Initializes this view model with the given collection of deleted objects
    /// </summary>
    /// <param name="deletedObjects">The collection of deleted objects for which to fix references.</param>
    public void Initialize(IEnumerable<T> deletedObjects)
    {
        ArgumentNullException.ThrowIfNull(deletedObjects);

        foreach (var deletedObject in deletedObjects)
        {
            var referencers = FindReferencers(deletedObject);
            foreach (var referencer in referencers)
            {
                if (referencer.Value.Count > 0)
                    referencersMap.Add(new ReferenceTarget(deletedObject, referencer.Key), referencer.Value);
            }
        }

        // Go to the first object to fix.
        NextReferenceIssue();
    }

    /// <summary>
    /// Updates the enabled status of the commands.
    /// </summary>
    public void UpdateCommands()
    {
        if (UseSingleReplace)
        {
            FixReferencesCommand.IsEnabled = SingleReplacementObject != null;
        }
        else
        {
            FixReferencesCommand.IsEnabled = CurrentReplacements.All(x => x.ReplacementObject != null || x.DontFixReference);
        }
    }

    /// <summary>
    /// Indicates whether the given object is one of the deleted objects to fix.
    /// </summary>
    /// <param name="obj">The object to check.</param>
    /// <returns><c>True</c> if the object is one of the deleted objects to fix, <c>False</c> otherwise.</returns>
    public bool IsInObjectsToFixList(T obj)
    {
        return referencersMap.Any(x => x.Key.ReferencedObject == obj);
    }

    /// <summary>
    /// Picks up an object to replace a reference to a deleted object in the given object to fix.
    /// </summary>
    /// <param name="objectToFix">The object for which a new object must be pick up to replace a reference.</param>
    /// <param name="propertyType">The type of the property or field in case it is more abstract than the object itself</param>
    /// <returns>A replacement object, or <c>null</c> if the user cancelled.</returns>
    public abstract Task<T?> PickupObject(T objectToFix, Type propertyType);

    /// <summary>
    /// Builds the lists of objects that reference the given deleted object. The return value of this method is an enumeration of key-value pairs,
    /// where the key can be either the deleted object, or a specific member of the deleted object, and the value is the list of objects
    /// that reference the deleted object (if the key is the deleted object itself), or the specific member of the deleted object.
    /// </summary>
    /// <param name="deletedObject">The deleted object for which to find referencers.</param>
    /// <returns>The list of referencers for the object to fix.</returns>
    protected abstract IEnumerable<KeyValuePair<object, List<T>>> FindReferencers(T deletedObject);

    /// <summary>
    /// Builds the list of <see cref="ReferenceReplacementViewModel{T}"/> for the given referencer object.
    /// </summary>
    /// <param name="referencer">The object that references the deleted object.</param>
    /// <param name="referencedMember">The deleted object, or the member of it that is referenced.</param>
    /// <returns>The replacements to resolve for the referencer.</returns>
    protected abstract IEnumerable<ReferenceReplacementViewModel<T>> GetReplacementsForReferencer(T referencer, object referencedMember);

    /// <summary>
    /// Applies the fixes on the actual objects
    /// </summary>
    public void ProcessFixes()
    {
        foreach (var refToFix in referencesToFix)
        {
            refToFix.FixReference();
        }
        foreach (var refToClear in referencesToClear)
        {
            refToClear.ClearReference();
        }
    }

    private bool NextReferenceIssue()
    {
        currentObjectToReplaceEnumerator ??= referencersMap.GetEnumerator();

        // Check if we have processed all object to fix. If so, tell the view to close.
        if (!currentObjectToReplaceEnumerator.MoveNext())
        {
            IsCompleted = true;
            Completed?.Invoke(this, EventArgs.Empty);
            return false;
        }

        ++FixedObjectsCount;

        CurrentObjectToReplace = currentObjectToReplaceEnumerator.Current.Key.ReferencedObject;
        CurrentReplacements.Clear();
        foreach (var referencer in currentObjectToReplaceEnumerator.Current.Value)
        {
            CurrentReplacements.AddRange(GetReplacementsForReferencer(referencer, currentObjectToReplaceEnumerator.Current.Key.ReferencedMember));
        }
        UpdateReplacements();
        return true;
    }

    private async Task PickupSingleReplacementObject()
    {
        if (currentObjectToReplaceEnumerator is null || IsCompleted)
            return;

        var current = currentObjectToReplaceEnumerator.Current.Key;
        var propType = current.ReferencedObject.GetType(); // We could find the type that is the most abstract and can be assigned to every replacement as well ...
        var selectedObject = await PickupObject(current.ReferencedObject, propType);
        if (selectedObject != null)
        {
            SingleReplacementObject = selectedObject;
        }
    }

    private void UpdateReplacements()
    {
        foreach (var replacement in CurrentReplacements)
        {
            replacement.ReplacementObject = SingleReplacementObject;
        }
        UpdateCommands();
    }

    private bool FixReferences()
    {
        if (UseSingleReplace)
        {
            UpdateReplacements();
        }

        referencesToFix.AddRange(CurrentReplacements);
        return !NextReferenceIssue();
    }

    private bool ClearReferences()
    {
        referencesToClear.AddRange(CurrentReplacements);
        return !NextReferenceIssue();
    }

    private void ClearAllReferences()
    {
        bool isFinished;
        do
        {
            isFinished = ClearReferences();
        }
        while (!isFinished);
    }
}
