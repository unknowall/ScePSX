using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using DiscordRPC;
using DiscordRPC.Logging;

namespace ScePSX.ThirdParty.DiscordRPC
{
    public class RPCManager : IDisposable
    {
        private static RPCManager _instance;
        public static RPCManager Instance => _instance ??= new RPCManager();

        private DiscordRpcClient _client;
        private bool _initialized = false;
        private bool _disposed = false;

        private string _gameName = "";
        private string _diskId = "";
        private bool _isPaused = false;
        private Stopwatch _playTimeStopwatch;
        private string _platformName = "";

        private const string APP_ID = "1482446005763834111"; //test id, change in prod

        public void Initialize()
        {
            if (_initialized) return;

            _platformName = GetPlatformName();

            try
            {
                _client = new DiscordRpcClient(APP_ID)
                {
                    Logger = new ConsoleLogger(LogLevel.Warning, true)
                };

                _client.OnReady += (sender, e) =>
                {
                    Console.WriteLine("[DiscordRPC] Ready! User: {0}", e.User.Username);
                };

                _client.OnPresenceUpdate += (sender, e) =>
                {
                    Console.WriteLine("[DiscordRPC] Presence updated");
                };

                _client.OnError += (sender, e) =>
                {
                    Console.WriteLine("[DiscordRPC] Error: {0} - {1}", e.Code, e.Message);
                };

                _client.Initialize();
                _playTimeStopwatch = new Stopwatch();
                _initialized = true;
                UpdatePresence();
                Console.WriteLine("[DiscordRPC] Initialized successfully on {0}", _platformName);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DiscordRPC] Failed to initialize: {ex.Message}");
            }
        }

        private string GetPlatformName()
        {
            if (OperatingSystem.IsWindows())
                return "Windows";
            if (OperatingSystem.IsLinux())
                return "Linux";
            if (OperatingSystem.IsMacOS())
                return "macOS";
            if (OperatingSystem.IsAndroid())
                return "Android";
            if (OperatingSystem.IsIOS())
                return "iOS"; //i dont think this will be ever the case but just in case

            return RuntimeInformation.OSDescription;
        }

        public void StartGame(string gameName, string diskId)
        {
            if (!_initialized || _client == null) return;

            _gameName = gameName;
            _diskId = diskId;
            _isPaused = false;
            _playTimeStopwatch.Restart();

            UpdatePresence();
        }

        public void SetPaused(bool paused)
        {
            Console.WriteLine($"[DiscordRPC] SetPaused called: {paused}, initialized: {_initialized}, client: {_client != null}, diskId: '{_diskId}'");
            if (!_initialized || _client == null || string.IsNullOrEmpty(_diskId)) return;

            _isPaused = paused;

            if (paused)
            {
                _playTimeStopwatch.Stop();
            }
            else
            {
                _playTimeStopwatch.Start();
            }

            UpdatePresence();
        }

        public void StopGame()
        {
            if (!_initialized || _client == null) return;

            _playTimeStopwatch.Stop();
            _client.ClearPresence();

            _gameName = "";
            _diskId = "";
            _isPaused = false;
        }

        private void UpdatePresence()
        {
            if (_client == null) return;

            bool isIdle = string.IsNullOrEmpty(_gameName);
            bool isInGame = !isIdle && !_isPaused;

            var presence = new RichPresence()
            {
                Details = isIdle ? "Menu" : _gameName,
                State = _isPaused ? $"Paused | {_platformName}" : (isIdle ? $"Idle | {_platformName}" : $"{FormatPlayTime(_playTimeStopwatch.Elapsed)} | {_platformName}"),
                Timestamps = isInGame ? new Timestamps(DateTime.UtcNow) : new Timestamps()
            };

            if (!isIdle)
            {
                presence.Assets = new Assets()
                {
                    LargeImageKey = "logo",
                    LargeImageText = "PlayStation 1 Emulator",
                    SmallImageKey = _isPaused ? "paused" : "playing",
                    SmallImageText = _isPaused ? "Paused" : "In Game"
                };
            }

            if (!string.IsNullOrEmpty(_diskId))
            {
                presence.Party = new Party()
                {
                    ID = _diskId,
                    Size = 1,
                    Max = 1
                };
            }

            _client.SetPresence(presence);
        }

        private string FormatPlayTime(TimeSpan time)
        {
            if (time.TotalHours >= 1)
                return $"Playing for {(int)time.TotalHours}h {time.Minutes}m";
            else if (time.TotalMinutes >= 1)
                return $"Playing for {(int)time.TotalMinutes}m {time.Seconds}s";
            else
                return "Starting...";
        }

        public void Shutdown()
        {
            if (!_initialized || _client == null) return;

            try
            {
                _client.Dispose();
                _client = null;
                _initialized = false;
                Console.WriteLine("[DiscordRPC] Shutdown complete");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DiscordRPC] Shutdown error: {ex.Message}");
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                Shutdown();
            }

            _disposed = true;
        }

        ~RPCManager()
        {
            Dispose(false);
        }
    }
}