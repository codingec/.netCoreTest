CREATE TABLE Users (
  Id INTEGER PRIMARY KEY,
  UserName TEXT,
  Email TEXT,
  EmailConfirmed INTEGER NOT NULL DEFAULT 0,
  PasswordHash TEXT,
  PhoneNumber TEXT,
  TwoFactorEnabled INTEGER NOT NULL DEFAULT 0,
  CreatedAt TEXT DEFAULT (datetime('now'))
);

CREATE INDEX IX_Users_Email ON Users (Email);

CREATE TABLE Roles (
  Id TEXT PRIMARY KEY,
  Name TEXT
);

CREATE TABLE UserRoles (
  UserId INTEGER NOT NULL,
  RoleId TEXT NOT NULL,
  PRIMARY KEY (UserId, RoleId),
  FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
  FOREIGN KEY (RoleId) REFERENCES Roles(Id)  ON DELETE CASCADE
);

CREATE TABLE UserClaims (
  Id      INTEGER PRIMARY KEY AUTOINCREMENT,
  UserId  INTEGER NOT NULL,
  ClaimType  TEXT,
  ClaimValue TEXT,
  FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
);

CREATE TABLE RoleClaims (
  Id INTEGER PRIMARY KEY AUTOINCREMENT,
  RoleId TEXT NOT NULL,
  ClaimType TEXT,
  ClaimValue TEXT,
  FOREIGN KEY (RoleId) REFERENCES Roles(Id) ON DELETE CASCADE
);

CREATE TABLE UserLogins (
  LoginProvider TEXT NOT NULL,
  ProviderKey   TEXT NOT NULL, 
  ProviderDisplayName TEXT,
  UserId INTEGER NOT NULL,
  PRIMARY KEY (LoginProvider, ProviderKey),
  FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
);

CREATE TABLE UserTokens (
  UserId INTEGER NOT NULL,
  LoginProvider TEXT NOT NULL,
  Name TEXT NOT NULL,
  Value TEXT,
  PRIMARY KEY (UserId, LoginProvider, Name),
  FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
);

CREATE TABLE Employees (
  ID INTEGER PRIMARY KEY,
  first_name TEXT NOT NULL,
  last_name  TEXT NOT NULL,
  email TEXT,
  phone TEXT,
  created_at TEXT DEFAULT (datetime('now')),
  user_id INTEGER,
  FOREIGN KEY (user_id) REFERENCES Users(Id)
);

CREATE UNIQUE INDEX UQ_Employees_User ON Employees(user_id);

CREATE TABLE Office (
  ID INTEGER PRIMARY KEY,
  name TEXT NOT NULL,
  image TEXT NOT NULL
);

CREATE TABLE Service (
  ID INTEGER PRIMARY KEY,
  office_id  INTEGER NOT NULL,
  name TEXT NOT NULL,
  description TEXT,
  quantity INTEGER,
  max_bookings_week INTEGER,
  booking_per_time  INTEGER,
  min_max TEXT,
  image TEXT,
  created_at TEXT DEFAULT (datetime('now')),
  FOREIGN KEY (office_id) REFERENCES Office(ID)
);

CREATE TABLE Service_Details (
  ID INTEGER PRIMARY KEY,
  service_id INTEGER NOT NULL,
  name TEXT NOT NULL,
  description TEXT,
  capacity INTEGER,
  icon TEXT,
  created_at TEXT DEFAULT (datetime('now')),
  FOREIGN KEY (service_id) REFERENCES Service(ID)
);

CREATE TABLE Booking (
  ID INTEGER PRIMARY KEY,
  service_detail_id INTEGER NOT NULL,
  employee_id INTEGER NOT NULL,
  start_datetime TEXT NOT NULL,
  end_datetime TEXT NOT NULL,
  created_at TEXT DEFAULT (datetime('now')),
  FOREIGN KEY (service_detail_id) REFERENCES Service_Details(ID),
  FOREIGN KEY (employee_id) REFERENCES Employees(ID)
);
