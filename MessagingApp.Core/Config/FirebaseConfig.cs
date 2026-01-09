using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;
using System;
using System.IO;
using System.Collections.Generic;
using System.Text.Json;

namespace MessagingApp.Config
{
    /// <summary>
    /// Firebase configuration and initialization
    /// </summary>
    public static class FirebaseConfig
    {
        private static FirebaseApp? _firebaseApp;
        private static FirestoreDb? _firestoreDb;
        private static readonly object _lock = new object();

        /// <summary>
        /// Firebase project ID - UPDATE THIS with your Firebase project ID
        /// </summary>
        public const string ProjectId = "nt106-messagingapp"; // Fallback only (prefer auto-detect)

        private sealed record ClientConfig(string? WebApiKey, string? ProjectId, string? StorageBucket);

        private static ClientConfig? TryLoadClientConfig()
        {
            const string fileName = "firebase-client-config.json";

            try
            {
                var baseDir = AppContext.BaseDirectory;
                var candidates = new List<string>
                {
                    Path.Combine(baseDir, "Config", fileName),
                    Path.Combine(baseDir, fileName),
                };

                DirectoryInfo? dir = new DirectoryInfo(baseDir);
                for (int i = 0; i < 8 && dir != null; i++)
                {
                    candidates.Add(Path.Combine(dir.FullName, "Config", fileName));
                    candidates.Add(Path.Combine(dir.FullName, "MessagingApp", "Config", fileName));
                    candidates.Add(Path.Combine(dir.FullName, "MessagingApp.Core", "Config", fileName));
                    candidates.Add(Path.Combine(dir.FullName, "3Mess", "Config", fileName));
                    dir = dir.Parent;
                }

                foreach (var path in candidates)
                {
                    if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                        continue;

                    var json = File.ReadAllText(path);
                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;

                    static string? TryGetString(JsonElement root, string name)
                    {
                        if (root.TryGetProperty(name, out var prop))
                        {
                            var value = prop.GetString();
                            if (!string.IsNullOrWhiteSpace(value)) return value.Trim();
                        }
                        return null;
                    }

                    var webApiKey = TryGetString(root, "webApiKey") ?? TryGetString(root, "apiKey");
                    var projectId = TryGetString(root, "projectId");
                    var storageBucket = TryGetString(root, "storageBucket");
                    return new ClientConfig(webApiKey, projectId, storageBucket);
                }
            }
            catch
            {
                // ignore malformed config
            }

            return null;
        }

        private static string? TryReadProjectIdFromServiceAccountJson(string path)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return null;
                using var doc = JsonDocument.Parse(File.ReadAllText(path));
                if (doc.RootElement.TryGetProperty("project_id", out var p))
                {
                    var v = p.GetString();
                    if (!string.IsNullOrWhiteSpace(v)) return v.Trim();
                }
            }
            catch { }
            return null;
        }

        /// <summary>
        /// Resolved Firebase project id.
        /// Priority: client config (projectId) -> env var FIREBASE_PROJECT_ID/GOOGLE_CLOUD_PROJECT -> service account JSON -> fallback const.
        /// </summary>
        public static string ResolvedProjectId
        {
            get
            {
                var fromClientConfig = TryLoadClientConfig()?.ProjectId;
                if (!string.IsNullOrWhiteSpace(fromClientConfig)) return fromClientConfig;

                var env = Environment.GetEnvironmentVariable("FIREBASE_PROJECT_ID");
                if (string.IsNullOrWhiteSpace(env)) env = Environment.GetEnvironmentVariable("GOOGLE_CLOUD_PROJECT");
                if (!string.IsNullOrWhiteSpace(env)) return env.Trim();

                var fromSa = TryReadProjectIdFromServiceAccountJson(CredentialsPath);
                if (!string.IsNullOrWhiteSpace(fromSa)) return fromSa;

                return ProjectId;
            }
        }

        private static GoogleCredential LoadCredential(string path)
        {
            // GoogleCredential.FromFile is marked obsolete in newer Google.Apis.Auth versions.
            // Prefer the CredentialFactory API, but keep a safe fallback.
            try
            {
                return CredentialFactory.FromFile<ServiceAccountCredential>(path).ToGoogleCredential();
            }
            catch
            {
#pragma warning disable CS0618
                return GoogleCredential.FromFile(path);
#pragma warning restore CS0618
            }
        }

        /// <summary>
        /// Firebase Storage bucket.
        /// You can override by setting env var FIREBASE_STORAGE_BUCKET (recommended).
        /// Default fallback is "{ProjectId}.appspot.com".
        /// </summary>
        public static string StorageBucket
        {
            get
            {
                string? env = Environment.GetEnvironmentVariable("FIREBASE_STORAGE_BUCKET");
                if (!string.IsNullOrWhiteSpace(env))
                {
                    return env.Trim();
                }

                var fromClientConfig = TryLoadClientConfig()?.StorageBucket;
                if (!string.IsNullOrWhiteSpace(fromClientConfig))
                {
                    return fromClientConfig;
                }

                return ResolvedProjectId + ".appspot.com";
            }
        }

        /// <summary>
        /// Firebase Web API Key (used for client-side email/password sign-in via Identity Toolkit REST API).
        /// Set via env var FIREBASE_WEB_API_KEY (or FIREBASE_API_KEY).
        /// </summary>
        public static string? WebApiKey
        {
            get
            {
                // 1) Prefer a local config file shipped with the app
                //    so end users don't need to set environment variables per machine.
                var fromClientConfig = TryLoadClientConfig()?.WebApiKey;
                if (!string.IsNullOrWhiteSpace(fromClientConfig)) return fromClientConfig;

                // 2) Fallback to environment variables (useful for dev/test)
                string? key = Environment.GetEnvironmentVariable("FIREBASE_WEB_API_KEY");
                if (string.IsNullOrWhiteSpace(key))
                {
                    key = Environment.GetEnvironmentVariable("FIREBASE_API_KEY");
                }

                if (string.IsNullOrWhiteSpace(key))
                {
                    return null;
                }

                return key.Trim();
            }
        }

        /// <summary>
        /// Path to Firebase credentials JSON file
        /// </summary>
        private static string CredentialsPath
        {
            get
            {
                // Check environment variable first
                string? envPath = Environment.GetEnvironmentVariable("FIREBASE_CREDENTIALS");
                if (!string.IsNullOrWhiteSpace(envPath) && File.Exists(envPath))
                {
                    return envPath;
                }

                // Try to locate credentials relative to current executable / repo structure.
                var discovered = TryFindCredentialsPath();
                if (!string.IsNullOrWhiteSpace(discovered))
                {
                    return discovered;
                }

                // Fallback: BaseDirectory/Config
                return Path.Combine(AppContext.BaseDirectory, "Config", "firebase-credentials.json");
            }
        }

        /// <summary>
        /// Get the resolved credentials path (service account JSON). Useful for Google Cloud clients (e.g., Storage).
        /// </summary>
        public static string GetCredentialsPath()
        {
            return CredentialsPath;
        }

        private static string? TryFindCredentialsPath()
        {
            const string fileName = "firebase-credentials.json";
            var baseDir = AppContext.BaseDirectory;

            // Common locations to probe (works when running from MessagingApp.UI/bin/...)
            var candidates = new List<string>
            {
                Path.Combine(baseDir, "Config", fileName),
                Path.Combine(baseDir, fileName),
            };

            // Walk upward a few levels and probe Config/ and common repo folders
            DirectoryInfo? dir = new DirectoryInfo(baseDir);
            for (int i = 0; i < 8 && dir != null; i++)
            {
                candidates.Add(Path.Combine(dir.FullName, "Config", fileName));
                candidates.Add(Path.Combine(dir.FullName, "MessagingApp", "Config", fileName));
                candidates.Add(Path.Combine(dir.FullName, "MessagingApp.Core", "Config", fileName));
                candidates.Add(Path.Combine(dir.FullName, "3Mess", "Config", fileName));
                dir = dir.Parent;
            }

            foreach (var path in candidates)
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
                    {
                        return path;
                    }
                }
                catch
                {
                    // ignore invalid paths
                }
            }

            return null;
        }

        /// <summary>
        /// Initialize Firebase Admin SDK
        /// </summary>
        public static void Initialize()
        {
            lock (_lock)
            {
                if (_firebaseApp != null)
                {
                    Console.WriteLine("Firebase already initialized.");
                    return;
                }

                try
                {
                    if (!File.Exists(CredentialsPath))
                    {
                        throw new FileNotFoundException(
                            $"Firebase credentials file not found at: {CredentialsPath}\n\n" +
                            "Tip: Bạn có thể đặt biến môi trường FIREBASE_CREDENTIALS trỏ tới file JSON service account.\n\n" +
                            "Please follow these steps:\n" +
                            "1. Go to Firebase Console (console.firebase.google.com)\n" +
                            "2. Select your project\n" +
                            "3. Go to Project Settings > Service Accounts\n" +
                            "4. Click 'Generate New Private Key'\n" +
                            "5. Save the JSON file as 'firebase-credentials.json' in the Config folder\n\n" +
                            "See FIREBASE_SETUP.md for detailed instructions."
                        );
                    }

                    _firebaseApp = FirebaseApp.Create(new AppOptions
                    {
                        Credential = LoadCredential(CredentialsPath),
                        ProjectId = ResolvedProjectId
                    });

                    Console.WriteLine("Firebase initialized successfully.");
                }
                catch (Exception ex)
                {
                    throw new Exception($"Failed to initialize Firebase: {ex.Message}", ex);
                }
            }
        }

        /// <summary>
        /// Get Firestore database instance
        /// </summary>
        public static FirestoreDb GetFirestoreDb()
        {
            lock (_lock)
            {
                if (_firestoreDb == null)
                {
                    if (_firebaseApp == null)
                    {
                        Initialize();
                    }

                    try
                    {
                        if (!File.Exists(CredentialsPath))
                        {
                            throw new FileNotFoundException($"Credentials file not found: {CredentialsPath}");
                        }

                        Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", CredentialsPath);
                        _firestoreDb = FirestoreDb.Create(ResolvedProjectId);
                        Console.WriteLine("Firestore database initialized.");
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"Failed to initialize Firestore: {ex.Message}", ex);
                    }
                }

                return _firestoreDb;
            }
        }

        /// <summary>
        /// Get Firebase App instance
        /// </summary>
        public static FirebaseApp GetApp()
        {
            if (_firebaseApp == null)
            {
                Initialize();
            }
            return _firebaseApp!;
        }

        /// <summary>
        /// Test Firebase connection
        /// </summary>
        public static bool TestConnection()
        {
            try
            {
                var db = GetFirestoreDb();
                // Try to access a dummy collection to test connection
                var testRef = db.Collection("_test");
                Console.WriteLine("Firebase connection test successful.");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Firebase connection test failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Check if Firebase is initialized
        /// </summary>
        public static bool IsInitialized => _firebaseApp != null;
    }
}
