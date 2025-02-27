namespace TranslateWithDictCC.ViewModels
{
	class SubjectViewModel : ViewModel
	{
		public int Count { get; }
		public string Subject { get; }

        bool isFilterActive;

        public bool IsFilterActive
        {
            get => isFilterActive;
            set => SetProperty(ref isFilterActive, value);
        }

        public string Description { get; }

        public SubjectViewModel(int count, string subject, string description)
        {
            Count = count;
            Subject = subject;
            Description = description;
        }
    }
}
