using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.ApplicationModel.Resources;
using System;
using System.Threading;
using System.Threading.Tasks;
using TranslateWithDictCC.Services;
using TranslateWithDictCC.ViewModels;
using Windows.Media.Playback;
using Windows.Networking.Connectivity;

namespace TranslateWithDictCC;

class AudioPlayer
{
    readonly AudioRecordingFetcher audioRecordingFetcher;
    readonly DialogService dialogService;

    bool currentlyPlayingAudioRecordingWord2;

    readonly MediaPlayer mediaPlayer = new MediaPlayer();
    readonly SemaphoreSlim audioPlayerSemaphore = new SemaphoreSlim(1);
    DispatcherQueue? dispatcherQueue = null;

    public DictionaryEntryViewModel? CurrentlyPlayingAudioRecording { get; private set; }

    public AudioPlayer(AudioRecordingFetcher audioRecordingFetcher, DialogService dialogService)
    {
        this.audioRecordingFetcher = audioRecordingFetcher;
        this.dialogService = dialogService;

        mediaPlayer.AudioCategory = MediaPlayerAudioCategory.Speech;
        mediaPlayer.MediaOpened += MediaPlayer_MediaOpened;
        mediaPlayer.MediaEnded += MediaPlayer_MediaEnded;
        mediaPlayer.MediaFailed += MediaPlayer_MediaFailed;
        mediaPlayer.AutoPlay = true;
    }

    public async Task PlayAudioRecording(DictionaryEntryViewModel dictionaryEntryViewModel, bool word2)
    {
        if (dispatcherQueue == null)
            dispatcherQueue = DispatcherQueue.GetForCurrentThread();

        if (!IsInternetAccessAvailable())
        {
            ResourceLoader resourceLoader = new ResourceLoader();

            ContentDialog contentDialog = new ContentDialog()
            {
                Title = resourceLoader.GetString("No_Internet_Access_Title"),
                Content = resourceLoader.GetString("No_Internet_Access_Body"),
                CloseButtonText = "OK"
            };

            await dialogService.ShowDialogAsync(contentDialog);
            return;
        }

        try
        {
            await audioPlayerSemaphore.WaitAsync();

            if (CurrentlyPlayingAudioRecording != null)
            {
                mediaPlayer.Pause();
                SetState(CurrentlyPlayingAudioRecording, currentlyPlayingAudioRecordingWord2, AudioRecordingState.Available);
                CurrentlyPlayingAudioRecording = null;
            }

            Uri? audioUri = await audioRecordingFetcher.GetAudioRecordingUri(dictionaryEntryViewModel, word2);

            if (audioUri != null)
            {
                CurrentlyPlayingAudioRecording = dictionaryEntryViewModel;
                currentlyPlayingAudioRecordingWord2 = word2;

                mediaPlayer.SetUriSource(audioUri);
            }
            else
                SetState(dictionaryEntryViewModel, word2, AudioRecordingState.Unavailable);
        }
        catch
        {
            ResourceLoader resourceLoader = new ResourceLoader();

            ContentDialog contentDialog = new ContentDialog()
            {
                Title = resourceLoader.GetString("Error_Retrieving_Audio_Recording_Title"),
                Content = resourceLoader.GetString("Error_Retrieving_Audio_Recording_Body"),
                CloseButtonText = "OK"
            };

            await dialogService.ShowDialogAsync(contentDialog);
        }
        finally
        {
            audioPlayerSemaphore.Release();
        }
    }

    public void Pause()
    {
        mediaPlayer.Pause();
    }

    private bool IsInternetAccessAvailable()
    {
        try
        {
            ConnectionProfile profile = NetworkInformation.GetInternetConnectionProfile();

            return profile != null && profile.GetNetworkConnectivityLevel() == NetworkConnectivityLevel.InternetAccess;
        }
        catch
        {
            return false;
        }
    }

    private void MediaPlayer_MediaOpened(MediaPlayer sender, object args)
    {
        dispatcherQueue!.TryEnqueue(() =>
        {
            if (CurrentlyPlayingAudioRecording != null)
                SetState(CurrentlyPlayingAudioRecording, currentlyPlayingAudioRecordingWord2, AudioRecordingState.Playing);
        });
    }

    private void MediaPlayer_MediaEnded(MediaPlayer sender, object args)
    {
        dispatcherQueue!.TryEnqueue(() =>
        {
            if (CurrentlyPlayingAudioRecording != null)
            {
                SetState(CurrentlyPlayingAudioRecording, currentlyPlayingAudioRecordingWord2, AudioRecordingState.Available);
                CurrentlyPlayingAudioRecording = null;
            }
        });
    }

    private void MediaPlayer_MediaFailed(MediaPlayer sender, MediaPlayerFailedEventArgs args)
    {
        dispatcherQueue!.TryEnqueue(() =>
        {
            if (CurrentlyPlayingAudioRecording != null)
            {
                SetState(CurrentlyPlayingAudioRecording, currentlyPlayingAudioRecordingWord2, AudioRecordingState.Unavailable);
                CurrentlyPlayingAudioRecording = null;
            }
        });
    }

    private void SetState(DictionaryEntryViewModel dictionaryEntryViewModel, bool word2, AudioRecordingState state)
    {
        if (word2)
            dictionaryEntryViewModel.AudioRecordingState2 = state;
        else
            dictionaryEntryViewModel.AudioRecordingState1 = state;
    }
}
