using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;
using System;
using System.IO;

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
        public const string ProjectId = "your-firebase-project-id"; // TODO: Replace with actual project ID

        /// <summary>
        /// Path to Firebase credentials JSON file
        /// </summary>
        private static string CredentialsPath
        {
            get
            {
                // Check environment variable first (for production)
                string? envPath = Environment.GetEnvironmentVariable("FIREBASE_CREDENTIALS");
                if (!string.IsNullOrEmpty(envPath) && File.Exists(envPath))
                    return envPath;

                // Default to Config folder
                string defaultPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "firebase-credentials.json");
                return defaultPath;
            }
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
                        Credential = GoogleCredential.FromFile(CredentialsPath),
                        ProjectId = ProjectId
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
                        _firestoreDb = FirestoreDb.Create(ProjectId);
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
