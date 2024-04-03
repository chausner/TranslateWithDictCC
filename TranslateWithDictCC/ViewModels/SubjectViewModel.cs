namespace TranslateWithDictCC.ViewModels
{
	class SubjectViewModel : ViewModel
	{
		public int Count { get; }
		public string Subject { get; }

        public SubjectViewModel(int count, string subject)
        {
            Count = count;
            Subject = subject;
        }
    }
}
