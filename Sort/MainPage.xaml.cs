using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Windows.UI.Xaml.Controls;
using Bogus;
using DynamicData;
using DynamicData.ReactiveUI;
using DynamicData.Binding;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Sort
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page, IViewFor<MainViewModel>
    {
        public MainPage()
        {
	        this.ViewModel = new MainViewModel();

            this.InitializeComponent();

	        this.OneWayBind(ViewModel, m => m.Models, v => v.PatientsListView.ItemsSource);

        }

	    object IViewFor.ViewModel
	    {
		    get => ViewModel;
		    set => ViewModel = (MainViewModel) value;
	    }

	    public MainViewModel ViewModel { get; set; }
    }

	public class MainViewModel : ReactiveObject
	{
		private CompositeDisposable _compositeDisposable = new CompositeDisposable();
		private SourceCache<ItemViewModel, string> _sourceCache = new SourceCache<ItemViewModel, string>(model => model.Id);

		public ReactiveList<ItemViewModel> Models { get; set; } = new ReactiveList<ItemViewModel>();


		public MainViewModel()
		{
			initializeData();

			var propertyChanges = _sourceCache.Connect().WhenPropertyChanged(p => p.LastChange)
				.Throttle(TimeSpan.FromMilliseconds(250))
				.Select(_ => Unit.Default);

			var comparer = SortExpressionComparer<ItemViewModel>.Ascending(l => l.LastChange);

			_sourceCache.Connect()
				.Sort(comparer, propertyChanges)
				.ObserveOn(RxApp.MainThreadScheduler)
				.Bind(Models)
				.Subscribe();

			initializeDataChanges();
		}

		private void initializeDataChanges()
		{
			Random r = new Random();

			Observable.Interval(TimeSpan.FromSeconds(1))
				.ObserveOn(RxApp.MainThreadScheduler)
				.Subscribe(_ =>
				{
					var randomIndex = r.Next(0, Models.Count - 1);
					var randomItem = Models.ElementAt(randomIndex);

					randomItem.LastChange = new Faker().Date.Recent(100);
				}).DisposeWith(_compositeDisposable);
		}

		private void initializeData()
		{
			Faker<ItemViewModel> faker = new Faker<ItemViewModel>();

			faker.Rules((f, m) =>
				{
					m.Id = Guid.NewGuid().ToString("N");
					m.Name = f.Person.FullName;
					m.Birthdate = f.Person.DateOfBirth;
					m.LastChange = f.Date.Recent(20);
				})
				.Generate(25)
				.ForEach(m => _sourceCache.AddOrUpdate(m));
		}
	}

	public class ItemViewModel : ReactiveObject
	{
		public string Id { get; set; }
		public string Name { get; set; }
		[Reactive]
		public DateTime Birthdate { get; set; }
		[Reactive]
		public DateTime LastChange { get; set; }
	}
}
