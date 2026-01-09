using Google.Cloud.Firestore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MessagingApp.Config;

namespace MessagingApp.Services
{
    /// <summary>
    /// Service for managing voice and video calls using Firestore for signaling
    /// </summary>
    public class FirestoreCallingService
    {
        private static FirestoreCallingService? _instance;
        public static FirestoreCallingService Instance => _instance ??= new FirestoreCallingService();

        private readonly FirestoreDb _db;

        private FirestoreCallingService()
        {
            _db = FirebaseConfig.GetFirestoreDb();
        }

        /// <summary>
        /// Initiate a call (1-1 or group)
        /// </summary>
        public async Task<(bool success, string message, string? callId)> InitiateCall(string callerId, List<string> participantIds, bool isVideo)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(callerId))
                {
                    return (false, "Caller ID is required", null);
                }

                if (participantIds == null || participantIds.Count == 0)
                {
                    return (false, "At least one participant is required", null);
                }

                // Ensure caller is not in participants list
                var participants = participantIds.Where(p => !string.IsNullOrWhiteSpace(p) && p != callerId).Distinct().ToList();

                if (participants.Count == 0)
                {
                    return (false, "No valid participants", null);
                }

                // Create call document
                var callRef = _db.Collection("calls").Document();
                var callData = new Dictionary<string, object>
                {
                    { "callerId", callerId },
                    { "participants", participants },
                    { "isVideo", isVideo },
                    { "status", "ringing" }, // ringing, active, ended, missed, rejected
                    { "createdAt", FieldValue.ServerTimestamp }
                };

                await callRef.SetAsync(callData);

                // Create participant states (for each participant)
                foreach (var participantId in participants)
                {
                    var stateRef = callRef.Collection("participantStates").Document(participantId);
                    await stateRef.SetAsync(new Dictionary<string, object>
                    {
                        { "userId", participantId },
                        { "status", "ringing" } // ringing, joined, declined, missed
                    });
                }

                // Caller state
                var callerStateRef = callRef.Collection("participantStates").Document(callerId);
                await callerStateRef.SetAsync(new Dictionary<string, object>
                {
                    { "userId", callerId },
                    { "status", "joined" },
                    { "joinedAt", FieldValue.ServerTimestamp }
                });

                return (true, "Call initiated successfully", callRef.Id);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initiating call: {ex.Message}");
                return (false, $"Error: {ex.Message}", null);
            }
        }

        /// <summary>
        /// Answer/join a call
        /// </summary>
        public async Task<(bool success, string message)> AnswerCall(string callId, string userId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(callId) || string.IsNullOrWhiteSpace(userId))
                {
                    return (false, "Call ID and User ID are required");
                }

                var callRef = _db.Collection("calls").Document(callId);
                var callSnap = await callRef.GetSnapshotAsync();

                if (!callSnap.Exists)
                {
                    return (false, "Call not found");
                }

                var status = callSnap.GetValue<string>("status");
                if (status != "ringing" && status != "active")
                {
                    return (false, "Call is no longer available");
                }

                // Update participant state
                var stateRef = callRef.Collection("participantStates").Document(userId);
                await stateRef.UpdateAsync(new Dictionary<string, object>
                {
                    { "status", "joined" },
                    { "joinedAt", FieldValue.ServerTimestamp }
                });

                // Update call status to active if it was ringing
                if (status == "ringing")
                {
                    await callRef.UpdateAsync(new Dictionary<string, object>
                    {
                        { "status", "active" },
                        { "startedAt", FieldValue.ServerTimestamp }
                    });
                }

                return (true, "Joined call successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error answering call: {ex.Message}");
                return (false, $"Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Reject/decline a call
        /// </summary>
        public async Task<(bool success, string message)> RejectCall(string callId, string userId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(callId) || string.IsNullOrWhiteSpace(userId))
                {
                    return (false, "Call ID and User ID are required");
                }

                var callRef = _db.Collection("calls").Document(callId);
                var stateRef = callRef.Collection("participantStates").Document(userId);

                await stateRef.UpdateAsync(new Dictionary<string, object>
                {
                    { "status", "declined" }
                });

                // Check if all participants declined
                var callSnap = await callRef.GetSnapshotAsync();
                var callerId2 = callSnap.GetValue<string>("callerId");
                var statesSnapshot = await callRef.Collection("participantStates").GetSnapshotAsync();
                bool allDeclined = statesSnapshot.Documents
                    .Where(d => d.Id != callerId2)
                    .All(d => d.GetValue<string>("status") == "declined");

                if (allDeclined)
                {
                    await callRef.UpdateAsync(new Dictionary<string, object>
                    {
                        { "status", "rejected" },
                        { "endedAt", FieldValue.ServerTimestamp }
                    });
                }

                return (true, "Call rejected");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error rejecting call: {ex.Message}");
                return (false, $"Error: {ex.Message}");
            }
        }

        /// <summary>
        /// End/leave a call
        /// </summary>
        public async Task<(bool success, string message)> EndCall(string callId, string userId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(callId) || string.IsNullOrWhiteSpace(userId))
                {
                    return (false, "Call ID and User ID are required");
                }

                var callRef = _db.Collection("calls").Document(callId);
                var callSnap = await callRef.GetSnapshotAsync();

                if (!callSnap.Exists)
                {
                    return (false, "Call not found");
                }

                var callerId = callSnap.GetValue<string>("callerId");

                // If caller ends the call, end for everyone
                if (userId == callerId)
                {
                    await callRef.UpdateAsync(new Dictionary<string, object>
                    {
                        { "status", "ended" },
                        { "endedAt", FieldValue.ServerTimestamp }
                    });
                }
                else
                {
                    // Just remove this participant
                    var stateRef = callRef.Collection("participantStates").Document(userId);
                    await stateRef.UpdateAsync(new Dictionary<string, object>
                    {
                        { "status", "left" }
                    });
                }

                return (true, "Call ended");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error ending call: {ex.Message}");
                return (false, $"Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Listen to incoming calls for a user
        /// </summary>
        public FirestoreChangeListener ListenToIncomingCalls(string userId, Action<Dictionary<string, object>> onCallReceived)
        {
            var query = _db.Collection("calls")
                .WhereArrayContains("participants", userId)
                .WhereEqualTo("status", "ringing");

            return query.Listen(snapshot =>
            {
                try
                {
                    foreach (var change in snapshot.Changes)
                    {
                        if (change.ChangeType == DocumentChange.Type.Added)
                        {
                            var callData = change.Document.ToDictionary();
                            callData["callId"] = change.Document.Id;
                            onCallReceived(callData);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in incoming calls listener: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// Listen to call state changes
        /// </summary>
        public FirestoreChangeListener ListenToCallState(string callId, Action<Dictionary<string, object>> onStateChanged)
        {
            var docRef = _db.Collection("calls").Document(callId);

            return docRef.Listen(snapshot =>
            {
                try
                {
                    if (snapshot.Exists)
                    {
                        var callData = snapshot.ToDictionary();
                        callData["callId"] = snapshot.Id;
                        onStateChanged(callData);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in call state listener: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// Send WebRTC signaling data (offer, answer, ice candidates)
        /// </summary>
        public async Task<(bool success, string message)> SendSignalingData(string callId, string fromUserId, string toUserId, string type, string data)
        {
            try
            {
                var signalRef = _db.Collection("calls").Document(callId)
                    .Collection("signaling").Document();

                await signalRef.SetAsync(new Dictionary<string, object>
                {
                    { "from", fromUserId },
                    { "to", toUserId },
                    { "type", type }, // offer, answer, ice-candidate
                    { "data", data },
                    { "timestamp", FieldValue.ServerTimestamp }
                });

                return (true, "Signaling data sent");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending signaling data: {ex.Message}");
                return (false, $"Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Listen to WebRTC signaling data for a user
        /// </summary>
        public FirestoreChangeListener ListenToSignaling(string callId, string userId, Action<Dictionary<string, object>> onSignalReceived)
        {
            var query = _db.Collection("calls").Document(callId)
                .Collection("signaling")
                .WhereEqualTo("to", userId);

            return query.Listen(snapshot =>
            {
                try
                {
                    foreach (var change in snapshot.Changes)
                    {
                        if (change.ChangeType == DocumentChange.Type.Added)
                        {
                            var signalData = change.Document.ToDictionary();
                            onSignalReceived(signalData);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in signaling listener: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// Get active call for a user
        /// </summary>
        public async Task<Dictionary<string, object>?> GetActiveCall(string userId)
        {
            try
            {
                // Check if user is caller in an active call
                var callerQuery = await _db.Collection("calls")
                    .WhereEqualTo("callerId", userId)
                    .WhereIn("status", new[] { "ringing", "active" })
                    .GetSnapshotAsync();

                if (callerQuery.Count > 0)
                {
                    var doc = callerQuery.Documents[0];
                    var data = doc.ToDictionary();
                    data["callId"] = doc.Id;
                    return data;
                }

                // Check if user is participant in an active call
                var participantQuery = await _db.Collection("calls")
                    .WhereArrayContains("participants", userId)
                    .WhereIn("status", new[] { "ringing", "active" })
                    .GetSnapshotAsync();

                if (participantQuery.Count > 0)
                {
                    var doc = participantQuery.Documents[0];
                    var data = doc.ToDictionary();
                    data["callId"] = doc.Id;
                    return data;
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting active call: {ex.Message}");
                return null;
            }
        }
    }
}
