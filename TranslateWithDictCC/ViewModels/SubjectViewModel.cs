namespace TranslateWithDictCC.ViewModels;

class SubjectViewModel : ViewModel
{
    public int Count { get; }
    public string Subject { get; }

    public bool IsFilterActive
    {
        get;
        set => SetProperty(ref field, value);
    }

    public string? Description { get; }

    public SubjectViewModel(int count, string subject, string? description)
    {
        Count = count;
        Subject = subject;
        Description = description;
    }
}
