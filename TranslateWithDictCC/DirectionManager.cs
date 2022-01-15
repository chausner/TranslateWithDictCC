using System;
using System.Linq;
using System.Threading.Tasks;
using TranslateWithDictCC.ViewModels;
using Windows.UI.StartScreen;

namespace TranslateWithDictCC
{
    class DirectionManager
    {
        public static DirectionManager Instance { get; } = new DirectionManager();

        public DirectionViewModel[] AvailableDirections { get; private set; }

        public DirectionViewModel SelectedDirection { get; private set; }

        private DirectionManager()
        {
        }

        public async Task UpdateDirection()
        {
            DirectionViewModel previouslySelected = SelectedDirection;

            SelectedDirection = null;

            AvailableDirections =
                (await DatabaseManager.Instance.GetDictionaries())
                .SelectMany(dict => new[] { new DirectionViewModel(dict, false), new DirectionViewModel(dict, true) })
                .OrderBy(dvm => dvm.OriginLanguage)
                .ToArray();

            SelectedDirection = null;

            if (previouslySelected != null)
                SelectedDirection = AvailableDirections.FirstOrDefault(dvm => dvm.Equals(previouslySelected));

            if (SelectedDirection == null)
                SelectedDirection = AvailableDirections.FirstOrDefault();

            await UpdateJumpList();
        }

        private async Task UpdateJumpList()
        {
            if (!JumpList.IsSupported())
                return;

            try
            {
                JumpList jumpList = await JumpList.LoadCurrentAsync();

                jumpList.SystemGroupKind = JumpListSystemGroupKind.None;
                jumpList.Items.Clear();

                foreach (DirectionViewModel directionViewModel in AvailableDirections)
                {
                    string itemName = string.Format("{0} → {1}", directionViewModel.OriginLanguage, directionViewModel.DestinationLanguage);
                    string arguments = "dict:" + directionViewModel.OriginLanguageCode + directionViewModel.DestinationLanguageCode;

                    JumpListItem jumpListItem = JumpListItem.CreateWithArguments(arguments, itemName);

                    jumpListItem.Logo = LanguageCodes.GetCountryFlagUri(directionViewModel.OriginLanguageCode);

                    jumpList.Items.Add(jumpListItem);
                }

                await jumpList.SaveAsync();
            }
            catch
            {
                // in rare cases, SaveAsync may fail with HRESULT 0x80070497: "Unable to remove the file to be replaced."
                // this appears to be a common problem without a solution, so we simply ignore any errors here
            }
        }
    }
}
