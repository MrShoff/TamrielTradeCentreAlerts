using LibAlerts;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TamrielTradeCentreScraper;

namespace ConsoleUI
{
    class FunctionCaller
    {
        private readonly string DefaultWatcherSaveLocation = "state.json";

        TimeSpan searchInterval = TimeSpan.FromSeconds(900);
        bool _watchersPaused = false;
        List<Watcher> _watchers;

        public void Load(string filepath = null)
        {
            try
            {
                Console.WriteLine($"Loading state file from {filepath ?? DefaultWatcherSaveLocation}");
                if (File.Exists(filepath ?? DefaultWatcherSaveLocation))
                {
                    string stateJson = File.ReadAllText(filepath ?? DefaultWatcherSaveLocation);
                    var loadedState = Newtonsoft.Json.JsonConvert.DeserializeObject<SystemState>(stateJson);
                    _watchers = loadedState.Watchers;
                    searchInterval = loadedState.SearchInterval;
                    Console.WriteLine($"Successfully loaded state");
                    PrintWatchers();
                }
                else
                {
                    Console.WriteLine($"Could not find {filepath ?? DefaultWatcherSaveLocation}");
                }                
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("An error occured during state file load.");
                Console.ResetColor();
            }
        }

        public void CallSiteScraper()
        {            
            Load();
            if (_watchers == null)
            {
                _watchers = new List<Watcher>();
            }
            if (_watchers.Count == 0)
            {
                Console.WriteLine("Entering new user setup");
                DoUserCommandConsole();
            }
            var initialInterval = searchInterval;
            searchInterval = TimeSpan.FromMinutes(2);
            //_watchers.Add(new Watcher(new Uri("https://us.tamrieltradecentre.com/pc/Trade/SearchResult?ItemID=&SearchType=Sell&ItemNamePattern=&ItemCategory1ID=5&ItemCategory2ID=24&ItemTraitID=&ItemQualityID=&IsChampionPoint=false&LevelMin=&LevelMax=&MasterWritVoucherMin=&MasterWritVoucherMax=&AmountMin=200&AmountMax=&PriceMin=&PriceMax=2.5"))
            //    { Description = "Bait Stack under 500 gold" });

            //_watchers.Add(new Watcher(new Uri("https://us.tamrieltradecentre.com/pc/Trade/SearchResult?SearchType=Sell&ItemID=&ItemNamePattern=&IsChampionPoint=false&LevelMin=&LevelMax=&ItemCategory1ID=5&ItemCategory2ID=29&ItemCategory3ID=&ItemQualityID=&ItemTraitID=&PriceMin=&PriceMax=100&MasterWritVoucherMin=&MasterWritVoucherMax=&AmountMin=100&AmountMax="))
            //    { Description = "White Fish stacks under 100g" });

            //_watchers.Add(new Watcher(new Uri("https://us.tamrieltradecentre.com/pc/Trade/SearchResult?ItemID=&SearchType=Sell&ItemNamePattern=&ItemCategory1ID=1&ItemCategory2ID=6&ItemCategory3ID=&ItemTraitID=15&ItemQualityID=&IsChampionPoint=false&LevelMin=&LevelMax=&MasterWritVoucherMin=&MasterWritVoucherMax=&AmountMin=&AmountMax=&PriceMin=&PriceMax=200"))
            //    { Description = "5 Intricate Jewelry under 200 gold", Stackable = false, MaxResultCount = 200, MinItemCount = 5, LastSeenThresholdMinutes = 120 });

            //_watchers.Add(new Watcher(6132) { Description = "Perfect Roe under 8000 gold", MaxPricePerUnit = 8000 });
            //_watchers.Add(new Watcher(511) { Description = "50+ stack Corn Flower under 300 gold each", MaxPricePerUnit = 300, MinStackSize = 50 });
            //_watchers.Add(new Watcher(211) { Description = "Dreugh Wax under 5000 gold", MaxPricePerUnit = 5000 });
            //_watchers.Add(new Watcher(5687) { Description = "Tempering Alloy under 4000 gold", MaxPricePerUnit = 4000 });
            //_watchers.Add(new Watcher(17523) { Description = "Powdered Mother of Pearl under 2000 gold", MaxPricePerUnit = 2000, MinStackSize = 5 });

            // White Fish
            /*
            _watchers.Add(new Watcher(6268) { Description = "Slaughterfish stacks", MaxPricePerUnit = 100, MinStackPrice = 10000 });
            _watchers.Add(new Watcher(2275) { Description = "Trodh stacks", MaxPricePerUnit = 100, MinStackPrice = 10000 });
            _watchers.Add(new Watcher(1220) { Description = "River Betty stacks", MaxPricePerUnit = 100, MinStackPrice = 10000 });
            _watchers.Add(new Watcher(801) { Description = "Salmon stacks", MaxPricePerUnit = 100, MinStackPrice = 10000 });
            _watchers.Add(new Watcher(941) { Description = "Spadetail stacks", MaxPricePerUnit = 100, MinStackPrice = 10000 });
            _watchers.Add(new Watcher(3482) { Description = "Silverside Perch stacks", MaxPricePerUnit = 100, MinStackPrice = 10000 });
            _watchers.Add(new Watcher(4438) { Description = "Longfin stacks", MaxPricePerUnit = 100, MinStackPrice = 10000 });
            _watchers.Add(new Watcher(5579) { Description = "Dhufish stacks", MaxPricePerUnit = 100, MinStackPrice = 10000 });
            */
            int refreshCount = 0;
            while (true)
            {
                Stopwatch searchTime = Stopwatch.StartNew();
                //Console.Clear();
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("===========================================================================================");
                Console.WriteLine($"{DateTime.Now.ToString("hh:mm:ss")} Refreshing items on your watch list ");
                Console.WriteLine($"At any time, press a key to pause and enter the command console ");
                Task.Factory.StartNew(() => ListenForUserInput());
                Console.WriteLine("===========================================================================================");
                Console.ResetColor();

                for(int i = 0; i < _watchers.Count; i++)
                {
                    while (_watchersPaused)
                    {
                        Thread.Sleep(1000);
                    }
                    if (i < _watchers.Count)
                    {
                        int scrapeDelay = GetScrapeDelay((int)searchInterval.TotalMilliseconds, _watchers.Count);
                        _watchers[i].TryProcessWatcher(scrapeDelay);
                    }
                }

                searchTime.Stop();
                if (searchInterval > searchTime.Elapsed)
                {
                    Thread.Sleep(searchInterval - searchTime.Elapsed);
                }
                refreshCount++;
                if (refreshCount == 1) // first refresh is 2 minutes, then reset to standard interval
                {
                    searchInterval = initialInterval;
                }
            }
        }

        private void Save(string filepath = null)
        {
            try
            {
                string serializedWatchers = Newtonsoft.Json.JsonConvert.SerializeObject(new SystemState() { SearchInterval = searchInterval, Watchers = _watchers });
                File.WriteAllText(filepath ?? DefaultWatcherSaveLocation, serializedWatchers);
                Console.WriteLine("State saved");
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occured during Save");
            }
          
        }

        private void ListenForUserInput()
        {
            Console.ReadKey(true);
            _watchersPaused = true;
            foreach(var watcher in _watchers) { watcher.PauseProcessing = true; }

            DoUserCommandConsole();

            foreach (var watcher in _watchers) { watcher.PauseProcessing = false; }
            _watchersPaused = false;
            Save();
            ListenForUserInput();
        }

        private void DoUserCommandConsole()
        {
            Console.BackgroundColor = ConsoleColor.DarkBlue;
            Console.WriteLine("===========================================================================================");
            Console.WriteLine($"Console command menu. Please enter one of the following commands");
            Console.WriteLine("===========================================================================================");
            string input = string.Empty;
            while (input != "c" && input != "q")
            {
                Console.WriteLine("c,q - Quit command console and continue processing watchers");
                Console.WriteLine("add,addwatcher - Add a new item watcher to the list");
                Console.WriteLine("remove - Remove a watcher from the list");
                Console.WriteLine("view - View the list of active watchers");
                Console.WriteLine("setRefreshInterval - View and set the refresh interval");
                Console.WriteLine("wishIwerePlayingAC - Me too.");

                Console.Write("User input: ");
                input = Console.ReadLine().ToLower();
                switch(input)
                {
                    case "c":
                    case "q":
                        Console.WriteLine("Resuming");
                        break;
                    case "add":
                    case "addwatcher":
                        var newWatcher = GetWatcherAttributesFromUser();
                        _watchers.Add(newWatcher);
                        Console.WriteLine("Watcher added.");
                        PrintWatchers();
                        break;
                    case "remove":
                    case "removewatcher":
                        PrintWatchers();
                        Console.Write("Enter the index of the one you'd like to remove: ");
                        var removeIndex = Console.ReadLine();
                        if (int.TryParse(removeIndex, out int index))
                        {
                            index--;
                            if (index < _watchers.Count())
                            {
                                Console.WriteLine($"Removing {_watchers[index].ToString()}");
                                _watchers.RemoveAt(index);
                            }
                            else
                            {
                                Console.WriteLine($"There is no watcher #{index+1}");
                            }
                        }
                        break;
                    case "view":
                    case "viewwatchers":
                        PrintWatchers();
                        break;
                    case "setrefreshinterval":
                        Console.WriteLine($"Current refresh interval: {searchInterval.TotalMinutes} minutes");
                        Console.Write("Enter desired refresh interval in minutes: ");
                        string userInput = Console.ReadLine();
                        double dblOutput;
                        while (!double.TryParse(userInput, out dblOutput))
                        {
                            Console.Write("Search interval needs to be a positive number. Search interval: ");
                            userInput = Console.ReadLine();
                        }
                        if (dblOutput >= 0.0)
                        {
                            searchInterval = TimeSpan.FromMinutes(dblOutput);
                            Console.WriteLine($"Done. Refresh interval set to: {searchInterval.TotalMinutes} minutes");
                        }
                        break;
                    default:
                        Console.WriteLine($"{input} is not a recognized command");
                        break;
                }
            }

            Console.BackgroundColor = ConsoleColor.Black;
        }

        private Watcher GetWatcherAttributesFromUser()
        {
            Watcher newWatcher = null;
            Console.WriteLine("Adding a new item to the watch list.");
            string userInput;
            int integerOutput;

            // Get ItemID
            Console.Write("ItemID or Search URL: ");
            userInput = Console.ReadLine();
            while (!int.TryParse(userInput, out integerOutput))
            {
                if (Uri.TryCreate(userInput, UriKind.Absolute, out Uri uriOutput))
                {
                    newWatcher = new Watcher(uriOutput);
                    break;
                }
                Console.Write("Couldn't read ItemID or Search URL. Try again. ItemID or Search URL: ");
                userInput = Console.ReadLine();
            }
            newWatcher = newWatcher ?? new Watcher(integerOutput);

            // Get Description
            Console.Write("Description: ");
            newWatcher.Description = Console.ReadLine();

            if (newWatcher.SearchUrl == null)
            {
                // Get LastSeenThresholdMinutes
                Console.Write("LastSeenThresholdMinutes: ");
                userInput = Console.ReadLine();
                while (!int.TryParse(userInput, out integerOutput))
                {
                    Console.Write("LastSeenThresholdMinutes needs to be an integer. LastSeenThresholdMinutes: ");
                    userInput = Console.ReadLine();
                }
                newWatcher.LastSeenThresholdMinutes = integerOutput;

                // Get MaxPricePerUnit
                Console.Write($"MaxPricePerUnit: ");
                userInput = Console.ReadLine();
                while (!int.TryParse(userInput, out integerOutput))
                {
                    Console.Write("MaxPricePerUnit needs to be a number. MaxPricePerUnit: ");
                    userInput = Console.ReadLine();
                }
                newWatcher.MaxPricePerUnit = integerOutput > 9999999 ? 9999999 : integerOutput;

                // Get MinStackPrice
                Console.Write("MinStackSize: ");
                userInput = Console.ReadLine();
                while (!int.TryParse(userInput, out integerOutput))
                {
                    Console.Write("MinStackPrice needs to be an integer. MinStackSize: ");
                    userInput = Console.ReadLine();
                }
                newWatcher.MinStackSize = integerOutput < 1 ? 1 : integerOutput > 1000 ? 1000 : integerOutput;
            }
            

            return newWatcher;
        }


        private void PrintWatchers()
        {
            for(int i = 0; i < _watchers.Count; i++)
            {
                Console.WriteLine($"{i+1} - {_watchers[i].ToString()}");
            }
        }

        private int GetScrapeDelay(int searchIntervalMs, int totalWatchers)
        {
            int maxDelay = searchIntervalMs / totalWatchers * 2;
            int delay = new Random().Next(0, maxDelay);
            return delay;
        }

    }
}
