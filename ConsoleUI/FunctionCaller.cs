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
        TimeSpan searchInterval = TimeSpan.FromSeconds(300);
        bool _watchersPaused = false;
        List<Watcher> _watchers = new List<Watcher>();

        public void CallSiteScraper()
        {
            //watchers.Add(new Watcher(4909) { Description = "Crawlers under 3 gold", MaxPricePerUnit = 3, MinStackPrice = 600 });
            _watchers.Add(new Watcher(6132) { Description = "Perfect Roe under 8000 gold", MaxPricePerUnit = 8000 });
            _watchers.Add(new Watcher(511) { Description = "100+ stack Corn Flower under 250 gold each", MaxPricePerUnit = 250, MinStackPrice = 7500 });
            _watchers.Add(new Watcher(211) { Description = "Dreugh Wax under 5000 gold", MaxPricePerUnit = 5000 });
            _watchers.Add(new Watcher(5687) { Description = "Tempering Alloy under 4000 gold", MaxPricePerUnit = 4000 });

            // White Fish
            _watchers.Add(new Watcher(6268) { Description = "Slaughterfish stacks", MaxPricePerUnit = 100, MinStackPrice = 10000 });
            _watchers.Add(new Watcher(2275) { Description = "Trodh stacks", MaxPricePerUnit = 100, MinStackPrice = 10000 });
            _watchers.Add(new Watcher(1220) { Description = "River Betty stacks", MaxPricePerUnit = 100, MinStackPrice = 10000 });
            _watchers.Add(new Watcher(801) { Description = "Salmon stacks", MaxPricePerUnit = 100, MinStackPrice = 10000 });
            _watchers.Add(new Watcher(941) { Description = "Spadetail stacks", MaxPricePerUnit = 100, MinStackPrice = 10000 });
            _watchers.Add(new Watcher(3482) { Description = "Silverside Perch stacks", MaxPricePerUnit = 100, MinStackPrice = 10000 });
            _watchers.Add(new Watcher(4438) { Description = "Longfin stacks", MaxPricePerUnit = 100, MinStackPrice = 10000 });
            _watchers.Add(new Watcher(5579) { Description = "Dhufish stacks", MaxPricePerUnit = 100, MinStackPrice = 10000 });

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

                string serializedWatchers = Newtonsoft.Json.JsonConvert.SerializeObject(_watchers);
                File.WriteAllText("watchers.txt", serializedWatchers);                
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
            ListenForUserInput();
        }

        private void DoUserCommandConsole()
        {
            Console.BackgroundColor = ConsoleColor.DarkBlue;
            Console.WriteLine("===========================================================================================");
            Console.WriteLine($"User input detected - pausing watchers. Please enter one of the following commands");
            Console.WriteLine("===========================================================================================");
            string input = string.Empty;
            while (input != "c" && input != "q")
            {
                Console.WriteLine("c,q - Quit command console and continue processing watchers");
                Console.WriteLine("add,addwatcher - Add a new item watcher to the list");
                Console.WriteLine("remove,removewatcher - Remove a watcher from the list");
                Console.WriteLine("view,viewwatchers - View the list of active watchers");

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
            Console.Write("ItemID: ");
            userInput = Console.ReadLine();
            while (!int.TryParse(userInput, out integerOutput))
            {
                Console.Write("ItemID needs to be an integer. ItemID: ");
                userInput = Console.ReadLine();
            }
            newWatcher = new Watcher(integerOutput);

            // Get Description
            Console.Write("Description: ");
            newWatcher.Description = Console.ReadLine();

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
                Console.Write("MaxPricePerUnit needs to be an integer. MaxPricePerUnit: ");
                userInput = Console.ReadLine();
            }
            newWatcher.MaxPricePerUnit = integerOutput > 9999999 ? 9999999 : integerOutput;

            // Get MaxResultCount
            Console.Write("MaxResultCount: ");
            userInput = Console.ReadLine();
            while (!int.TryParse(userInput, out integerOutput))
            {
                Console.Write("MaxResultCount needs to be an integer. MaxResultCount: ");
                userInput = Console.ReadLine();
            }
            newWatcher.MaxResultCount = integerOutput > 40 ? 40 : integerOutput;

            // Get MinStackPrice
            Console.Write("MinStackPrice: ");
            userInput = Console.ReadLine();
            while (!int.TryParse(userInput, out integerOutput))
            {
                Console.Write("MinStackPrice needs to be an integer. MinStackPrice: ");
                userInput = Console.ReadLine();
            }
            newWatcher.MinStackPrice = integerOutput < 0 ? 0 : integerOutput > 9999999 ? 9999999 : integerOutput;

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
