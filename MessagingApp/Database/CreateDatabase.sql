-- ===================================
-- Database Creation Script
-- Messaging Application Database
-- ===================================

USE master;
GO

-- Drop database if exists
IF EXISTS (SELECT name FROM sys.databases WHERE name = N'MessagingAppDB')
BEGIN
    ALTER DATABASE MessagingAppDB SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE MessagingAppDB;
END
GO

-- Create database
CREATE DATABASE MessagingAppDB;
GO

USE MessagingAppDB;
GO

-- ===================================
-- Table: Users
-- ===================================
CREATE TABLE Users (
    UserID INT PRIMARY KEY IDENTITY(1,1),
    Username NVARCHAR(50) UNIQUE NOT NULL,
    Email NVARCHAR(100) UNIQUE NOT NULL,
    PasswordHash NVARCHAR(255) NOT NULL,
    FullName NVARCHAR(100),
    PhoneNumber NVARCHAR(20),
    Avatar NVARCHAR(255),
    Status NVARCHAR(50) DEFAULT 'Offline',
    Bio NVARCHAR(500),
    CreatedAt DATETIME DEFAULT GETDATE(),
    LastLogin DATETIME,
    IsActive BIT DEFAULT 1,
    CONSTRAINT CHK_Username_Length CHECK (LEN(Username) >= 3),
    CONSTRAINT CHK_Email_Format CHECK (Email LIKE '%@%.%')
);
GO

-- ===================================
-- Table: Friendships
-- ===================================
CREATE TABLE Friendships (
    FriendshipID INT PRIMARY KEY IDENTITY(1,1),
    UserID1 INT NOT NULL,
    UserID2 INT NOT NULL,
    Status NVARCHAR(20) DEFAULT 'Pending',
    RequestedBy INT NOT NULL,
    CreatedAt DATETIME DEFAULT GETDATE(),
    AcceptedAt DATETIME,
    CONSTRAINT FK_Friendships_User1 FOREIGN KEY (UserID1) REFERENCES Users(UserID),
    CONSTRAINT FK_Friendships_User2 FOREIGN KEY (UserID2) REFERENCES Users(UserID),
    CONSTRAINT FK_Friendships_RequestedBy FOREIGN KEY (RequestedBy) REFERENCES Users(UserID),
    CONSTRAINT CHK_Different_Users CHECK (UserID1 <> UserID2),
    CONSTRAINT CHK_Friendship_Status CHECK (Status IN ('Pending', 'Accepted', 'Blocked'))
);
GO

-- ===================================
-- Table: Conversations
-- ===================================
CREATE TABLE Conversations (
    ConversationID INT PRIMARY KEY IDENTITY(1,1),
    ConversationName NVARCHAR(100),
    IsGroup BIT DEFAULT 0,
    CreatedBy INT NOT NULL,
    CreatedAt DATETIME DEFAULT GETDATE(),
    LastMessageAt DATETIME,
    CONSTRAINT FK_Conversations_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES Users(UserID)
);
GO

-- ===================================
-- Table: ConversationParticipants
-- ===================================
CREATE TABLE ConversationParticipants (
    ParticipantID INT PRIMARY KEY IDENTITY(1,1),
    ConversationID INT NOT NULL,
    UserID INT NOT NULL,
    JoinedAt DATETIME DEFAULT GETDATE(),
    LeftAt DATETIME,
    IsActive BIT DEFAULT 1,
    CONSTRAINT FK_Participants_Conversation FOREIGN KEY (ConversationID) REFERENCES Conversations(ConversationID),
    CONSTRAINT FK_Participants_User FOREIGN KEY (UserID) REFERENCES Users(UserID)
);
GO

-- ===================================
-- Table: Messages
-- ===================================
CREATE TABLE Messages (
    MessageID INT PRIMARY KEY IDENTITY(1,1),
    ConversationID INT NOT NULL,
    SenderID INT NOT NULL,
    MessageText NVARCHAR(MAX),
    MessageType NVARCHAR(20) DEFAULT 'Text',
    AttachmentPath NVARCHAR(255),
    SentAt DATETIME DEFAULT GETDATE(),
    IsRead BIT DEFAULT 0,
    IsEdited BIT DEFAULT 0,
    IsDeleted BIT DEFAULT 0,
    CONSTRAINT FK_Messages_Conversation FOREIGN KEY (ConversationID) REFERENCES Conversations(ConversationID),
    CONSTRAINT FK_Messages_Sender FOREIGN KEY (SenderID) REFERENCES Users(UserID),
    CONSTRAINT CHK_Message_Type CHECK (MessageType IN ('Text', 'Image', 'File', 'Audio', 'Video'))
);
GO

-- ===================================
-- Table: CallHistory
-- ===================================
CREATE TABLE CallHistory (
    CallID INT PRIMARY KEY IDENTITY(1,1),
    CallerID INT NOT NULL,
    ReceiverID INT NOT NULL,
    CallType NVARCHAR(20) DEFAULT 'Voice',
    StartTime DATETIME DEFAULT GETDATE(),
    EndTime DATETIME,
    Duration INT DEFAULT 0,
    Status NVARCHAR(20) DEFAULT 'Completed',
    CONSTRAINT FK_Calls_Caller FOREIGN KEY (CallerID) REFERENCES Users(UserID),
    CONSTRAINT FK_Calls_Receiver FOREIGN KEY (ReceiverID) REFERENCES Users(UserID),
    CONSTRAINT CHK_Call_Type CHECK (CallType IN ('Voice', 'Video')),
    CONSTRAINT CHK_Call_Status CHECK (Status IN ('Completed', 'Missed', 'Rejected', 'Failed'))
);
GO

-- ===================================
-- Table: MessageReadStatus
-- ===================================
CREATE TABLE MessageReadStatus (
    ReadStatusID INT PRIMARY KEY IDENTITY(1,1),
    MessageID INT NOT NULL,
    UserID INT NOT NULL,
    ReadAt DATETIME DEFAULT GETDATE(),
    CONSTRAINT FK_ReadStatus_Message FOREIGN KEY (MessageID) REFERENCES Messages(MessageID),
    CONSTRAINT FK_ReadStatus_User FOREIGN KEY (UserID) REFERENCES Users(UserID)
);
GO

-- ===================================
-- Indexes for Performance
-- ===================================

-- Users indexes
CREATE INDEX idx_users_username ON Users(Username);
CREATE INDEX idx_users_email ON Users(Email);
CREATE INDEX idx_users_status ON Users(Status);
GO

-- Friendships indexes
CREATE INDEX idx_friendships_user1 ON Friendships(UserID1);
CREATE INDEX idx_friendships_user2 ON Friendships(UserID2);
CREATE INDEX idx_friendships_status ON Friendships(Status);
GO

-- ConversationParticipants indexes
CREATE INDEX idx_participants_conversation ON ConversationParticipants(ConversationID);
CREATE INDEX idx_participants_user ON ConversationParticipants(UserID);
GO

-- Messages indexes
CREATE INDEX idx_messages_conversation ON Messages(ConversationID);
CREATE INDEX idx_messages_sender ON Messages(SenderID);
CREATE INDEX idx_messages_sentat ON Messages(SentAt);
GO

-- CallHistory indexes
CREATE INDEX idx_calls_caller ON CallHistory(CallerID);
CREATE INDEX idx_calls_receiver ON CallHistory(ReceiverID);
CREATE INDEX idx_calls_starttime ON CallHistory(StartTime);
GO

-- ===================================
-- Sample Data for Testing
-- ===================================

-- Insert sample users (passwords are 'password123' hashed with simple method)
INSERT INTO Users (Username, Email, PasswordHash, FullName, PhoneNumber, Status)
VALUES 
    ('admin', 'admin@messaging.app', 'password123', N'Quản Trị Viên', '0900000001', 'Online'),
    ('user1', 'user1@messaging.app', 'password123', N'Nguyễn Văn A', '0900000002', 'Offline'),
    ('user2', 'user2@messaging.app', 'password123', N'Trần Thị B', '0900000003', 'Offline');
GO

PRINT 'Database created successfully!';
GO
