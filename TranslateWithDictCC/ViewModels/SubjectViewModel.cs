namespace TranslateWithDictCC.ViewModels;

class SubjectViewModel : ViewModel
{
    public int Count { get; }
    public string CanonicalSubject { get; }
    public string Subject { get; }

    public string? Description { get; }

    public SubjectViewModel(int count, string canonicalSubject, string subject, string? description)
    {
        Count = count;
        CanonicalSubject = canonicalSubject;
        Subject = subject;
        Description = description;
    }
}
