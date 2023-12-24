using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using ListProcesses.Models;
using Stylet;

namespace ListProcesses.Pages
{
    public class ShellViewModel : Screen
    {
        private string _logInfo = "";

        public string LogInfo { get => _logInfo; set => SetAndNotify(ref _logInfo, value); }

        private int _timeBetweenRefreshing = 3;
        public int TimeBetweenRefreshing
        {
            get => _timeBetweenRefreshing;
            set => SetAndNotify(ref _timeBetweenRefreshing, value);
        }

        private ProcessPriorityClass _priorityClass;
        public List<ProcessPriorityClass> PriorityClasses { get; init; }
        private int _selectedPriority;
        public int SelectedPriority
        {
            get => _selectedPriority;
            set
            {
                if (_selectedPriority == value) return;
                SetAndNotify(ref _selectedPriority, value);
                SetChosenProcessPriority(value);
            }
        }
        private void SetChosenProcessPriority(int priority)
        {
            _priorityClass = PriorityClasses.ElementAt(priority);
        }

        #region Simple Processes
        private CollectionViewSource _simpleProcessesView;

        /// <summary>
        /// CollectionViewSource of the simple processes used for the ListBox (Left bar list of all processes)
        /// </summary>
        public CollectionViewSource SimpleProcessesView
        {
            get => _simpleProcessesView;
            set => SetAndNotify(ref _simpleProcessesView, value);
        }

        private BindableCollection<SimpleProcess> _simpleProcessesCollection;

        /// <summary>
        /// BindableCollection of the simple processes used for the SimpleProcessesView
        /// </summary>
        public BindableCollection<SimpleProcess> SimpleProcessesCollection
        {
            get => _simpleProcessesCollection;
            set => SetAndNotify(ref _simpleProcessesCollection, value);
        }

        private string _filterByInput = "";

        public string FilterByInput
        {
            get => _filterByInput;
            set => SetAndNotify(ref _filterByInput, value);
        }

        private SimpleProcess _simpleProcess;

        /// <summary>
        /// Currently selected simple process for the parent and the list of its children
        /// </summary>
        public SimpleProcess CurrentProcess
        {
            get => _simpleProcess;
            set
            {
                if (!Busy)
                {
                    SetAndNotify(ref _simpleProcess, value);
                    SelectedProcessDetails();
                }
                else
                {
                    SetAndNotify(ref _simpleProcess, value);
                }
            }
        }

        private SimpleProcess _previousProcess;

        #endregion

        #region Detailed Processes
        private BindableCollection<DetailedProcess> _detailedProcessesCollection;

        /// <summary>
        /// BindableCollection of the detailed processes used for the ProcessesDetailedView
        /// </summary>
        public BindableCollection<DetailedProcess> DetailedProcessesCollection
        {
            get => _detailedProcessesCollection;
            set => SetAndNotify(ref _detailedProcessesCollection, value);
        }

        private CollectionViewSource _processesDetailedView;

        /// <summary>
        /// CollectionViewSource of the detailed processes used for the ListBox (Panel of detailed processes on the right)
        /// </summary>
        public CollectionViewSource ProcessesDetailedView
        {
            get => _processesDetailedView;
            set => SetAndNotify(ref _processesDetailedView, value);
        }

        private DetailedProcess _currentDetailedProcess;

        /// <summary>
        /// Currently selected detailed process for changing priority or killing it
        /// </summary>
        public DetailedProcess CurrentDetailedProcess 
        { 
            get => _currentDetailedProcess; 
            set => SetAndNotify(ref _currentDetailedProcess, value);
        }
        private DetailedProcess _previousDetailedProcess;

        #endregion

        #region Task handling
        private CancellationTokenSource cancellationTokenSource;
        private CancellationToken cancellationToken;
        private Task updateProcessListTask;
        #endregion

        private bool _busy = false;

        public bool Busy
        {
            get => _busy;
            set => SetAndNotify(ref _busy, value);
        }

        /// <summary>
        /// Setup and run the main functionality of the app, start refreshing simple processes
        /// </summary>
        public ShellViewModel()
        {
            PriorityClasses = Enum.GetValues(typeof(ProcessPriorityClass)).Cast<ProcessPriorityClass>().ToList();
            SimpleProcessesView = new CollectionViewSource();
            ProcessesDetailedView = new CollectionViewSource();
            SimpleProcessesCollection = new BindableCollection<SimpleProcess>(Process.GetProcesses().GetSimpleProcesses());
            SimpleProcessesView.Source = SimpleProcessesCollection;
            _simpleProcess = SimpleProcessesCollection[0];
            StartRefreshing();

            SimpleProcessesView.Filter += (s, e) =>
            {
                SimpleProcess search = (SimpleProcess)e.Item;
                if (search.Name.Contains(FilterByInput) || $"{search.Id}".Contains(FilterByInput))
                {
                    e.Accepted = true;
                    return;
                }
                e.Accepted = false;

            };
            SimpleProcessesView.SortDescriptions.Add(new SortDescription("Id", ListSortDirection.Ascending));
            ProcessesDetailedView.SortDescriptions.Add(new SortDescription("HierarchyType",
                ListSortDirection.Ascending));
        }


        /// <summary>
        /// Setups DetailedProcessesCollection and ProcessesDetailedView 
        /// with DetailedProcesses asociated with selected SimpleProcess
        /// </summary>
        public void SelectedProcessDetails()
        {
            if (CurrentProcess == null || Busy == true)
            {
                CurrentDetailedProcess = _previousDetailedProcess;
                return;
            }

            Busy = true;
            Task.Factory.StartNew(() =>
            {
                DetailedProcessesCollection = new BindableCollection<DetailedProcess>(CurrentProcess.GetSystemProcesses());
                
                Execute.OnUIThread(() =>
                {
                    ProcessesDetailedView.Source = DetailedProcessesCollection;
                    CanDetailedListSortBy = true;
                    Busy = false;
                });

            });

        }

        public void RefreshSimpleView()
        {
            SimpleProcessesView.View.Refresh();

        }

        private bool _canDetailedListSortBy = false;
        public bool CanDetailedListSortBy
        {
            get => _canDetailedListSortBy;
            set => SetAndNotify(ref _canDetailedListSortBy, value);
        }
        public void DetailedListSortBy(string sort)
        {

            if (ProcessesDetailedView.SortDescriptions[0].PropertyName == sort)
            {
                if (ProcessesDetailedView.SortDescriptions[0].Direction == ListSortDirection.Ascending)
                {
                    ProcessesDetailedView.SortDescriptions[0] = new SortDescription(sort, ListSortDirection.Descending);
                    return;
                }
                ProcessesDetailedView.SortDescriptions[0] = new SortDescription(sort, ListSortDirection.Ascending);
                return;
            }
            ProcessesDetailedView.SortDescriptions[0] = new SortDescription(sort, ListSortDirection.Ascending);
        }

        public void SimpleListSortBy(string sort)
        {
            if (SimpleProcessesView.SortDescriptions[0].PropertyName == sort)
            {
                if (SimpleProcessesView.SortDescriptions[0].Direction == ListSortDirection.Ascending)
                {
                    SimpleProcessesView.SortDescriptions[0] = new SortDescription(sort, ListSortDirection.Descending);
                    return;
                }
                SimpleProcessesView.SortDescriptions[0] = new SortDescription(sort, ListSortDirection.Ascending);
                return;
            }
            SimpleProcessesView.SortDescriptions[0] = new SortDescription(sort, ListSortDirection.Ascending);
        }

        /// <summary>
        /// Checks if the app can access details of the process or perform any actions on it
        /// </summary>
        public void ValidateAccess()
        {
            if (CurrentDetailedProcess == null)
            {
                CanKillProcess = false;
                CanChangePriority = false;
                return;
            }

            if (CurrentDetailedProcess.PriorityAccessed)
            {
                CanChangePriority = true;
                CanKillProcess = true;
                return;
            }

            CanChangePriority = false;
            CanKillProcess = false;
        }

        private bool _canKillProcess = false;
        public bool CanKillProcess
        {
            get => _canKillProcess;
            set => SetAndNotify(ref _canKillProcess, value);
        }
        public void KillProcess()
        {
            var temp = CurrentDetailedProcess.Kill();
            if (temp)
            {
                LogInfo = "Successfully killed process " + CurrentDetailedProcess.Id;
                SelectedProcessDetails();
                return;
            }
            LogInfo = "Couldn't kill process " + CurrentDetailedProcess.Id;
        }

        private bool _canChangePriority = false;
        public bool CanChangePriority
        {
            get => _canChangePriority;
            set => SetAndNotify(ref _canChangePriority, value);
        }
        public void ChangePriority()
        {
            LogInfo = "Changing priority of "+ CurrentDetailedProcess.Id;
            var temp = CurrentDetailedProcess.ChangePriority(_priorityClass);
            if (temp)
            {
                LogInfo = "Successfully changed priority of " + CurrentDetailedProcess.Id; 
                ProcessesDetailedView.View.Refresh();
                return;
            }
            LogInfo = "Couldn't change priority of " + CurrentDetailedProcess.Id;
        }

        private bool _canStartRefreshing;

        public bool CanStartRefreshing
        {
            get => _canStartRefreshing;
            set => SetAndNotify(ref _canStartRefreshing, value);
        }


        /// <summary>
        /// Runs a task that refreshes the list of the SimpleProcesses
        /// with selected time between refreshes or 3s if invalid
        /// </summary>
        public void StartRefreshing()
        {
            CanStartRefreshing = false;
            CanRefreshProcesses = false;
            cancellationTokenSource = new CancellationTokenSource();
            cancellationToken = cancellationTokenSource.Token;
            updateProcessListTask = Task.Factory.StartNew(() =>
            {
                CanStopRefreshing = true;
                LogInfo = "Started updating processes.";
                while (true)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        Busy = true;
                        break;
                    }

                    RefreshProcesses();

                    if (TimeBetweenRefreshing > 0) Thread.Sleep(TimeBetweenRefreshing * 1000);
                    else
                    {
                        Thread.Sleep(3000);
                        TimeBetweenRefreshing = 3;
                    }
                }
            }, cancellationTokenSource.Token);
        }

        private bool _canStopRefreshing;

        public bool CanStopRefreshing
        {
            get => _canStopRefreshing;
            set => SetAndNotify(ref _canStopRefreshing, value);
        }
        public async void StopRefreshing()
        {
            cancellationTokenSource.Cancel();
            await updateProcessListTask;
            updateProcessListTask.Dispose();

            Busy = false;
            CanStartRefreshing = true;
            CanStopRefreshing = false;
            CanRefreshProcesses = true;
            LogInfo = "Stopped refreshing";
        }
        private bool _canRefreshProcesses;

        public bool CanRefreshProcesses
        {
            get => _canRefreshProcesses;
            set => SetAndNotify(ref _canRefreshProcesses, value);
        }

        /// <summary>
        /// Refresh all SimpleProcesses (all processes) and update the page
        /// </summary>
        public void RefreshProcesses()
        {
            if (!Busy)
            {
                Busy = true;
                Execute.OnUIThread(CommandManager.InvalidateRequerySuggested);
                if (_simpleProcess != null)
                {
                    _previousProcess = _simpleProcess;
                    if (CurrentDetailedProcess != null) _previousDetailedProcess = CurrentDetailedProcess;
                }

                SimpleProcessesCollection =
                    new BindableCollection<SimpleProcess>(Process.GetProcesses().GetSimpleProcesses());

                Execute.OnUIThread(() =>
                {
                    IDisposable freezeDetailedProcessView = null;
                    IDisposable freezeSimpleProcessView = SimpleProcessesView.DeferRefresh();

                    if (ProcessesDetailedView != null) freezeDetailedProcessView = ProcessesDetailedView.DeferRefresh();

                    SimpleProcessesView.Source = SimpleProcessesCollection;

                    freezeSimpleProcessView.Dispose();

                    if (_previousProcess == null)
                    {
                        freezeDetailedProcessView?.Dispose();
                        return;
                    }

                    freezeDetailedProcessView?.Dispose();
                    CurrentProcess = _previousProcess;

                    if (CurrentDetailedProcess != _previousDetailedProcess) 
                        CurrentDetailedProcess = _previousDetailedProcess;
                    else _currentDetailedProcess = _previousDetailedProcess;

                    Busy = false;
                    Execute.OnUIThread(CommandManager.InvalidateRequerySuggested);
                });

            }
        }
    }
}
