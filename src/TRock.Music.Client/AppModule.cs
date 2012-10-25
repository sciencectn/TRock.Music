using System.Collections;
using System.Linq;
using System.Threading;

using Microsoft.Expression.Interactivity.Core;
using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Prism.Modularity;
using Microsoft.Practices.Prism.Regions;

namespace TRock.Music.Client
{
    public class AppModule : IModule
    {
        #region Fields

        private readonly IVoteableQueue<ISongStream> _queue;
        private readonly IRegionManager _regionManager;
        private readonly ISongPlayer _songPlayer;
        private readonly ISongStreamPlayer _streamPlayer;

        #endregion Fields

        #region Constructors

        public AppModule(
            IRegionManager regionManager,
            IVoteableQueue<ISongStream> queue,
            ISongPlayer songPlayer,
            ISongStreamPlayer streamPlayer)
        {
            _regionManager = regionManager;

            _queue = queue;
            _queue.ItemAdded += QueueOnItemAdded;

            _streamPlayer = streamPlayer;
            _streamPlayer.NextSong += StreamPlayerNextSongChanged;

            _songPlayer = songPlayer;
            _songPlayer.CurrentSongCompleted += SongPlayerOnCurrentSongCompleted;
        }

        #endregion Constructors

        #region Methods

        public void Initialize()
        {
            _regionManager.RequestNavigate("MainRegion", typeof(SearchView).Name);

            AppCommands.NavigateBackCommand.RegisterCommand(new DelegateCommand(ExecuteNavigateBack));
            AppCommands.PlayCommand.RegisterCommand(new ActionCommand(ExecutePlay));
            AppCommands.NextCommand.RegisterCommand(new ActionCommand(ExecuteNext));
        }

        private void SongPlayerOnCurrentSongCompleted(object sender, SongEventArgs songEventArgs)
        {
            if (!_streamPlayer.Next(CancellationToken.None))
            {
                VoteableQueueItem<ISongStream> item;
                if (_queue.TryGetNext(out item))
                {
                    _streamPlayer.CurrentStream = item.Item;
                    _streamPlayer.Next(CancellationToken.None);
                }
            }
        }

        private void StreamPlayerNextSongChanged(object sender, SongEventArgs eventArgs)
        {
            _songPlayer.Start(eventArgs.Song);
        }

        private void QueueOnItemAdded(object sender, QueueEventArgs<VoteableQueueItem<ISongStream>> e)
        {
            if (_queue.IsInFront(e.Item))
            {
                _streamPlayer.CurrentStream = e.Item.Item;
                _streamPlayer.Next(CancellationToken.None);
            }
        }

        private void ExecuteNavigateBack()
        {
            _regionManager.Regions["MainRegion"].NavigationService.Journal.GoBack();
        }

        private void ExecutePlay(object argument)
        {
            if (argument is Song)
            {
                _queue.Enqueue(new SingleSongStream(((Song) argument)));
            }
            else if (argument is IEnumerable)
            {
                var songs = ((IEnumerable) argument).OfType<Song>().ToArray();

                if (songs.Length > 0)
                {
                    _queue.Enqueue(new MultiSongStream(songs));
                }
            }
        }

        private void ExecuteNext(object argument)
        {
            if (!_streamPlayer.Next(CancellationToken.None))
            {
                VoteableQueueItem<ISongStream> item;
                if (_queue.TryGetNext(out item))
                {
                    _streamPlayer.CurrentStream = item.Item;
                    _streamPlayer.Next(CancellationToken.None);
                }
            }
        }

        #endregion Methods
    }
}