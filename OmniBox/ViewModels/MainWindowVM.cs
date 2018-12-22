﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using Newtonsoft.Json;

namespace OmniBox
{
    public class MainWindowVM : ViewModelBase
    {
        #region Events

        public static event Action RequestMinimizeEvent;

        public static void RequestMinimize()
        {
            RequestMinimizeEvent?.Invoke();
        }

        public static event Action RequestActivateEvent;

        public static void RequestActivate()
        {
            RequestActivateEvent?.Invoke();
        }

        public static event Action RequestToggleShowEvent;

        public static void RequestToggleShow()
        {
            RequestToggleShowEvent?.Invoke();
        }

        public static event Action<bool> RequestChangeResultsVisibilityEvent;

        public static void RequestChangeResultsVisibility(bool value)
        {
            RequestChangeResultsVisibilityEvent?.Invoke(value);
        }

        public static event Action<int> RequestResetAnimationFrameRateEvent;

        public static void RequestResetAnimationFrameRate(int value)
        {
            RequestResetAnimationFrameRateEvent?.Invoke(value);
        }

        #endregion Events

        #region Properties

        // ANIMATIONS
        public int AnimationFrameRate { get; private set; } = 60;

        // WINDOW SIZE
        public static double WindowWidth => SystemParameters.PrimaryScreenWidth * 0.4;

        public static double WindowHeight => SystemParameters.PrimaryScreenHeight * 0.075;

        // WINDOW POSITION
        public static double WindowXPosition => (SystemParameters.PrimaryScreenWidth - WindowWidth) / 2;

        public static double WindowYPosition => (SystemParameters.PrimaryScreenHeight * 0.2) - (WindowHeight / 2);

        // WINDOW COLORS
        public SolidColorBrush BackgroundBrush { get; private set; } = new SolidColorBrush(Color.FromArgb(200, 200, 200, 200));

        public SolidColorBrush BorderBrush { get; private set; }

        // SEARCH BAR COLORS
        public SolidColorBrush OmniBoxForegroundBrush { get; private set; } = new SolidColorBrush(Color.FromArgb(250, 100, 100, 100));

        // WINDOW BORDER
        public CornerRadius CornerRadius { get; private set; } = new CornerRadius(10);

        public Thickness BorderThickness { get; private set; } = new Thickness(5);

        public Thickness OmniBoxPadding { get; private set; } = new Thickness(5);

        // SEARCH BAR TEXT STYLE
        public double OmniBoxFontSize { get; private set; }

        public FontFamily OmniBoxFontFamily { get; private set; } = new FontFamily("Calibri");

        public SolidColorBrush OmniBoxCaretBrush { get; private set; } = new SolidColorBrush(Color.FromArgb(0, 250, 250, 250));

        // RESULTS
        public ObservableCollection<Result> Results { get; } = new ObservableCollection<Result>();

        public int SelectedIndex { get; set; } = -1;

        // RESULTS STYLE
        public SolidColorBrush ResultsForegroundBrush { get; private set; } = new SolidColorBrush(Color.FromArgb(250, 100, 100, 100));

        public double ResultsFontSize { get; private set; }

        public SolidColorBrush ResultsDescriptorForegroundBrush { get; private set; } = new SolidColorBrush(Color.FromArgb(250, 150, 150, 150));

        public double ResultsDescriptorFontSize { get; private set; }

        // MODE STYLE
        public Thickness ModeBorderThickness { get; private set; } = new Thickness(3, 0, 0, 0);

        public SolidColorBrush ModeBorderBrush { get; private set; } = new SolidColorBrush(Color.FromArgb(200, 200, 200, 200));

        // SEARCH
        private HotKey ActivationHotKey { get; set; }

        public string OmniBoxContent
        {
            get => _OmniBoxContent;

            set
            {
                _OmniBoxContent = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(WatermarkVisibility));
                OmniBoxContentChanged();
            }
        }
        private string _OmniBoxContent = string.Empty;

        // MODE
        public string ModeText => currentCommand.mode;

        public string CurrentModeIconPath => Path.Combine(
#if DEBUG
                    "..", "..",
#endif
                    "Resources", currentCommand.image);

        // WATERMARK
        public Visibility WatermarkVisibility => String.IsNullOrEmpty(OmniBoxContent) ? Visibility.Visible : Visibility.Hidden;

        // DEBUG MODE
        public static bool DebugMode { get; private set; }

        public static Visibility DebugModeVisibility => DebugMode ? Visibility.Visible : Visibility.Hidden;

        #endregion Properties

        #region Fields

        private Dictionary<string, Command> commands;

        //TODO save these back to the settings file
        private static readonly Dictionary<string, Action<string>> settingSetters
            = new Dictionary<string, Action<string>>()
        {
                { "defaultCommandPrefix", value => SettingsHelper.defaultCommandPrefix = value },
                { "appsPrefix", value => SettingsHelper.appsPrefix = value },
                { "settingsPrefix", value => SettingsHelper.settingsPrefix = value },
                { "animationFramerate", value => RequestResetAnimationFrameRate(int.Parse(value.Trim())) }
        };

        private Command currentCommand;
        private Command defaultCommand;

        #endregion Fields

        public MainWindowVM()
        {
            OmniBoxFontSize = (WindowHeight - (BorderThickness.Top + BorderThickness.Bottom + OmniBoxPadding.Top + OmniBoxPadding.Bottom)) * 0.65;
            ResultsFontSize = OmniBoxFontSize * 0.6;
            ResultsDescriptorFontSize = ResultsFontSize * 0.8;
            ActivationHotKey = new HotKey(Key.Space, KeyModifier.Alt, ActivationShortcutDetected, true);

            Intitialize();
        }

        public void Intitialize()
        {
#if DEBUG
            var settingsFilePath = Path.Combine("..", "..", "Settings.ini");
#else
            var settingsFilePath = "Settings.ini";
#endif
            SettingsHelper.LoadSettings(settingsFilePath);
            AppHelper.CacheInstalledPrograms(SettingsHelper.appIndexLocation);

#if DEBUG
            var commandsDirectory = Path.Combine(Environment.CurrentDirectory, "..", "..", "Commands");
#else
            string commandsDirectory = Path.Combine(Environment.CurrentDirectory, "Commands");
#endif
            commands = JsonHelper.Parse(Directory.GetFiles(commandsDirectory, "*.json", SearchOption.AllDirectories)).ToDictionary(x => x.command, x => x);

            commands.Add(SettingsHelper.superSearchPrefix, new Command()
            { command = SettingsHelper.superSearchPrefix, mode = "SuperSearch", image = "atom.png", description = "Super search" });

            //TODO should this be in a static constructor?
            commands.Add(SettingsHelper.settingsPrefix, new Command()
            { command = SettingsHelper.settingsPrefix, mode = "ChangeSettings", image = "settings.png", description = "Customize the OmniBox's settings." });
            commands.Add(SettingsHelper.commandsPrefix, new Command()
            { command = SettingsHelper.commandsPrefix, mode = "Command", image = "command.png", description = "Run an OmniBox command." });
            commands.Add(SettingsHelper.appsPrefix, new Command()
            { command = SettingsHelper.appsPrefix, mode = "App", image = "app.png", description = "Run an installed app.", _CustomResultsAction = AppHelper.GetResults });
            commands.Add(SettingsHelper.fileSearchPrefix, new Command()
            { command = SettingsHelper.fileSearchPrefix, mode = "FileSearch", image = "find.png", description = "Find a file in the indexed locations." });

            // SET DEFAULTS
            RequestResetAnimationFrameRate(SettingsHelper.animationFrameRate);

            defaultCommand = commands[SettingsHelper.defaultCommandPrefix];
            currentCommand = defaultCommand;

            //BindingOperations.EnableCollectionSynchronization(Results, resultsLock);

            OnPropertyChanged(nameof(ModeText));
            OnPropertyChanged(nameof(CurrentModeIconPath));
        }

        public void ActivationShortcutDetected()
        {
            Debug.Print("Show Toggled");

            RequestToggleShow();

            //var installed = new InstalledFontCollection();
            //var fonts = installed.Families;
        }

        private void OmniBoxContentChanged()
        {
            SetMode();

            GetResults();

            SelectedIndex = -1;
            OnPropertyChanged(nameof(SelectedIndex));

            // cache results per search for backspace?

            // Compute Results / Narrow results
            //if (OmniBoxContent.Length > lastComputedOmniBoxContent.Length)
            //    Results.Add(new Result(OmniBoxContent.Last().ToString(), "search.png"));
            //else if (OmniBoxContent.Length < lastComputedOmniBoxContent.Length)
            //    Results.RemoveAt(Results.Count - 1);

            //lastComputedOmniBoxContent = OmniBoxContent;

            // Find Best Match/first one, only for commands?
            //if (currentMode == Mode.Command)
            //    SuggestedCompletion = Commands.OrderBy(command => LevenshteinDistance(OmniBoxContent, command))
            //        .First().Remove(0, OmniBoxContent.Length - "/".Length);
        }

        //TODO Emoji search engine, selecting one copies it to the clipboard
        // Clipboard.SetText();
        // Clipboard.Set??

        //TODO use google http request array of relevance and the verbatim relevance score to get a 
        // percentage then use that to sort that result in with the rest of the results

        private void GetResults()
        {
            var computed = currentCommand.GetResults(OmniBoxContent);
            Results.Clear();
            foreach (var res in computed)
                Results.Add(res);
            RequestChangeResultsVisibility(Results.Any());
        }

        //TODO change default to some OmniBox super search or some shit, if default is not specified in settings
        private void SetMode()
        {
            var prefix = OmniBoxContent.Split(new[] { ' ' })[0];
            currentCommand = commands.TryGetValue(prefix, out var command) ? command : defaultCommand;

            OnPropertyChanged(nameof(ModeText));
            OnPropertyChanged(nameof(CurrentModeIconPath));
        }

        public void ResultClicked(Result result)
        {
            if (ExecuteCommand(result))
                RequestMinimize();

            OmniBoxContent = String.Empty;
        }

        public void PreviewSearchBoxKeyUp(Key key)
        {
            if (key == Key.Enter)
            {
                if (ExecuteCommand(GetSelectedResult()))
                    RequestMinimize();

                //else
                //    RequestShowDialog(ERROR);

                OmniBoxContent = String.Empty;
            }
            else if (key == Key.Escape)
            {
                RequestMinimize();

                OmniBoxContent = String.Empty;
            }
        }

        public Result? GetSelectedResult()
            => (SelectedIndex > -1) ? (Result?)Results[SelectedIndex] : null;

        //TODO use current mode
        //TODO check selected required and make use of selected
        // If selected required 
        //     if no selection
        //         use first
        //     else
        //         use selection
        // else 
        //     if selectedindex >= 0
        //         use selection
        //     else
        //         use construct
        private bool ExecuteCommand(Result? selected = null)
        {
            if (OmniBoxContent.StartsWith(SettingsHelper.fileSearchPrefix))
            {
                try
                {
                    throw new NotImplementedException();
                }
                catch (Exception)
                {
                    var message = "An error occured searching the specified location.";
                    //requestshowerror(message);
                }
                return false;
            }
            else if (OmniBoxContent.StartsWith(SettingsHelper.settingsPrefix))
            {
                try
                {
                    OmniBoxContent = OmniBoxContent.Remove(0, SettingsHelper.settingsPrefix.Length).TrimStart();
                    var pair = OmniBoxContent.Split(new[] { '=' });
                    if (settingSetters.TryGetValue(pair[0].Trim(), out var settingSetter))
                    {
                        settingSetter?.Invoke(pair[1].Trim());
                        return true;
                    }
                }
                catch (Exception)
                {
                    var message = "An error occured indexing the specified location.";
                    //requestshowerror(message);
                }
                return false;
            }
            else if (OmniBoxContent.StartsWith(SettingsHelper.commandsPrefix))
            {
                OmniBoxContent = OmniBoxContent.Remove(0, SettingsHelper.commandsPrefix.Length).TrimStart();

                // Get command with lowest index/selected item

                switch (OmniBoxContent.Split(new[] { ' ' })[0])
                {
                    case "Exit":
                        //RequestClose();
                        Application.Current.Shutdown();
                        break;

                    case "Restart":
                    {
                        //System.Diagnostics.Process.Start(Application.ExecutablePath);
                        //RequestClose()
                    }
                    break;
                }
            }
            else if (OmniBoxContent.StartsWith(SettingsHelper.appsPrefix))
            {
                // Potentially only allow execution of the selected app, need the results to work for that first though

                try
                {
                    OmniBoxContent = OmniBoxContent.Remove(0, SettingsHelper.appsPrefix.Length).TrimStart();

                    //TODO Execute the app with the lowest stringDiffIndex

                    if (selected == null)
                        selected = Results.FirstOrDefault();

//#if DEBUG
//                    var error =
//#endif
                    Process.Start(new ProcessStartInfo()
                    {
                        WindowStyle = ProcessWindowStyle.Hidden,
                        FileName = selected.Value.path
                        //Arguments = OmniBoxContent.Remove(0, selected.ResultText.Length)
//#if DEBUG
//                    ,
//                        RedirectStandardOutput = true,
//                        UseShellExecute = false
//#endif
                    })
                    //#if DEBUG
                    //                    .StandardOutput.ReadToEnd()
                    //#endif
                    ;
                    //#if DEBUG
                    //                    Debug.Print("Command execute output: " + error);
                    //#endif
                    return true;
                }
                catch (Exception ex)
                {
                    var message = "An error occured attempting to run the specified app.";
                    //requestshowerror(message);
                    throw;
                }

                return false;
            }
            else if (commands.TryGetValue(OmniBoxContent.Split(new[] { ' ' }, 2)[0], out var command)
                || commands.TryGetValue(SettingsHelper.defaultCommandPrefix, out command))
            {
                (var filePath, var arguments) = command.GetCommand(selected, OmniBoxContent);

                //DbHelper.RecordCommandUse(command, overrideContent ?? OmniBoxContent, OmniBoxContent);
//#if DEBUG
//                var error =
//#endif
                    Process.Start(new ProcessStartInfo()
                    {
                        WindowStyle = ProcessWindowStyle.Normal,
                        FileName = filePath,
                        Arguments = arguments

//#if DEBUG
//                    ,
//                        RedirectStandardOutput = true,
//                        UseShellExecute = false
//#endif
                    })
                    //#if DEBUG
                    //                    .StandardOutput.ReadToEnd()
                    //#endif
                    ;
                //#if DEBUG
                //                Debug.Print("Command execute output: " + error);
                //#endif
            }

            return true;
        }

        private void SetDebugMode(bool value)
        {
            DebugMode = value;
            OnPropertyChanged(nameof(DebugMode));
            OnPropertyChanged(nameof(DebugModeVisibility));
        }
    }
}
