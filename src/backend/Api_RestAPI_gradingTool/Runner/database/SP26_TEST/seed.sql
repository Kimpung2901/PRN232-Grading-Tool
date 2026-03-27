/****** Object:  Table [dbo].[LeopardAccount]    Script Date: 06/24/25 1:57:06 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[LeopardAccount](
	[AccountID] [int] IDENTITY(1,1) NOT NULL,
	[UserName] [nvarchar](50) NOT NULL,
	[Password] [nvarchar](100) NULL,
	[FullName] [nvarchar](100) NOT NULL,
	[Email] [nvarchar](150) NOT NULL,
	[Phone] [nvarchar](50) NOT NULL,
	[RoleId] [int] NOT NULL,
 CONSTRAINT [PK_LeopardAccount] PRIMARY KEY CLUSTERED 
(
	[AccountID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[LeopardProfile]    Script Date: 06/24/25 1:57:06 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[LeopardProfile](
	[LeopardProfileId] [int] IDENTITY(1,1) NOT NULL,
	[LeopardTypeId] [int] NOT NULL,
	[LeopardName] [nvarchar](150) NOT NULL,
	[Weight] [float] NOT NULL,
	[Characteristics] [nvarchar](2000) NOT NULL,
	[CareNeeds] [nvarchar](1500) NOT NULL,
	[ModifiedDate] [datetime] NOT NULL,
 CONSTRAINT [PK_LeopardProfile] PRIMARY KEY CLUSTERED 
(
	[LeopardProfileId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[LeopardType]    Script Date: 06/24/25 1:57:06 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[LeopardType](
	[LeopardTypeId] [int] IDENTITY(1,1) NOT NULL,
	[LeopardTypeName] [nvarchar](250) NULL,
	[Origin] [nvarchar](250) NULL,
	[Description] [nvarchar](1000) NULL,
 CONSTRAINT [PK_LeopardType] PRIMARY KEY CLUSTERED 
(
	[LeopardTypeId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
SET IDENTITY_INSERT [dbo].[LeopardAccount] ON 

INSERT [dbo].[LeopardAccount] ([AccountID], [UserName], [Password], [FullName], [Email], [Phone], [RoleId]) VALUES (1, N'admin', N'@1', N'admin', N'admin@leopard.com', N'09011223435', 1)
INSERT [dbo].[LeopardAccount] ([AccountID], [UserName], [Password], [FullName], [Email], [Phone], [RoleId]) VALUES (2, N'manager', N'@1', N'manager', N'manager@leopard.com', N'09011223440', 2)
INSERT [dbo].[LeopardAccount] ([AccountID], [UserName], [Password], [FullName], [Email], [Phone], [RoleId]) VALUES (3, N'staff', N'@1', N'staff', N'staff@leopard.com', N'09011223450', 3)
INSERT [dbo].[LeopardAccount] ([AccountID], [UserName], [Password], [FullName], [Email], [Phone], [RoleId]) VALUES (4, N'member', N'@1', N'member', N'member@leopard.com', N'09011223458', 4)
INSERT [dbo].[LeopardAccount] ([AccountID], [UserName], [Password], [FullName], [Email], [Phone], [RoleId]) VALUES (5, N'administrator', N'@1', N'administrator', N'administrator@leopard.com', N'09011223435', 5)
INSERT [dbo].[LeopardAccount] ([AccountID], [UserName], [Password], [FullName], [Email], [Phone], [RoleId]) VALUES (9, N'moderator', N'@1', N'moderator', N'moderator@leopard.com', N'09011223435', 6)
INSERT [dbo].[LeopardAccount] ([AccountID], [UserName], [Password], [FullName], [Email], [Phone], [RoleId]) VALUES (10, N'developer', N'@1', N'developer', N'developer@leopard.com', N'09011223435', 7)
SET IDENTITY_INSERT [dbo].[LeopardAccount] OFF
GO
SET IDENTITY_INSERT [dbo].[LeopardProfile] ON 

INSERT [dbo].[LeopardProfile] ([LeopardProfileId], [LeopardTypeId], [LeopardName], [Weight], [Characteristics], [CareNeeds], [ModifiedDate]) VALUES (1, 1, N'Panthera tigris tigris', 35, N'The leopard possesses a tawny or rusty yellow-colored coat with close-set rosettes and dark spots', N'These animals are classified as endangered by the IUCN', CAST(N'2025-06-20T00:00:00.000' AS DateTime))
INSERT [dbo].[LeopardProfile] ([LeopardProfileId], [LeopardTypeId], [LeopardName], [Weight], [Characteristics], [CareNeeds], [ModifiedDate]) VALUES (2, 1, N'Nepalaliis', 22, N'Females of this subspecies weigh around 29 kg, and males weigh around 56 kg', N'These animals are classified as endangered by the IUCN', CAST(N'2025-06-20T00:00:00.000' AS DateTime))
INSERT [dbo].[LeopardProfile] ([LeopardProfileId], [LeopardTypeId], [LeopardName], [Weight], [Characteristics], [CareNeeds], [ModifiedDate]) VALUES (3, 1, N'Bhutanient', 39, N'The Sri Lankan leopard has historically been found across a wide range of habitats on the island nation including arid scrub jungle, rainforest, upper highland forest, and dry evergreen monsoon forest', N'These animals are classified as endangered by the IUCN', CAST(N'2025-06-20T00:00:00.000' AS DateTime))
INSERT [dbo].[LeopardProfile] ([LeopardProfileId], [LeopardTypeId], [LeopardName], [Weight], [Characteristics], [CareNeeds], [ModifiedDate]) VALUES (4, 2, N'Bengal', 33, N'The leopards are either completely black due to a recessive phenotype or have the usual spotted coat', N'These animals are classified as endangered by the IUCN', CAST(N'2025-06-20T00:00:00.000' AS DateTime))
INSERT [dbo].[LeopardProfile] ([LeopardProfileId], [LeopardTypeId], [LeopardName], [Weight], [Characteristics], [CareNeeds], [ModifiedDate]) VALUES (5, 2, N'Ekaterina', 30, N'The Javan leopard is critically endangered, and only about 250 individuals survive in protected habitats in their range', N'These animals are classified as endangered by the IUCN', CAST(N'2025-06-20T00:00:00.000' AS DateTime))
INSERT [dbo].[LeopardProfile] ([LeopardProfileId], [LeopardTypeId], [LeopardName], [Weight], [Characteristics], [CareNeeds], [ModifiedDate]) VALUES (6, 2, N'Sierra', 31, N'Depletion of the prey base, poaching, habitat loss and also conflicts with humans have resulted in a rapid downfall in the numbers of the Javan leopard', N'These animals are classified as endangered by the IUCN', CAST(N'2025-06-20T00:00:00.000' AS DateTime))
INSERT [dbo].[LeopardProfile] ([LeopardProfileId], [LeopardTypeId], [LeopardName], [Weight], [Characteristics], [CareNeeds], [ModifiedDate]) VALUES (7, 3, N'Panthera', 17, N'Like most other wildlife in the region, the leopard faces threats due to habitat loss and poaching for illegal wildlife trade', N'These animals are classified as endangered by the IUCN', CAST(N'2025-06-20T00:00:00.000' AS DateTime))
INSERT [dbo].[LeopardProfile] ([LeopardProfileId], [LeopardTypeId], [LeopardName], [Weight], [Characteristics], [CareNeeds], [ModifiedDate]) VALUES (8, 3, N'Sumatra', 20, N'A report produced in 2016 came as a shock to conservationists since it revealed that there are only about 400 to 1,000 breeding adults of the Indochinese leopard left in the wild', N'These animals are classified as endangered by the IUCN', CAST(N'2025-06-20T00:00:00.000' AS DateTime))
INSERT [dbo].[LeopardProfile] ([LeopardProfileId], [LeopardTypeId], [LeopardName], [Weight], [Characteristics], [CareNeeds], [ModifiedDate]) VALUES (9, 3, N'Baliance', 20, N'The Indochinese leopard appears in a predominantly black form south of the Kra Isthmus and a predominantly spotted form north of the Isthmus', N'These animals are classified as endangered by the IUCN', CAST(N'2025-06-20T00:00:00.000' AS DateTime))
SET IDENTITY_INSERT [dbo].[LeopardProfile] OFF
GO
SET IDENTITY_INSERT [dbo].[LeopardType] ON 

INSERT [dbo].[LeopardType] ([LeopardTypeId], [LeopardTypeName], [Origin], [Description]) VALUES (1, N'Sri Lankan Leopard', N'Nepal', N'The Sri Lankan leopard (Panthera pardus kotiya) is a leopard subspecies that is native to Sri Lanka')
INSERT [dbo].[LeopardType] ([LeopardTypeId], [LeopardTypeName], [Origin], [Description]) VALUES (2, N'Javan Leopard', N'Georgia', N'The highly threatened Javan leopard (Panthera pardus melas) is endemic to the Indonesian island of Java')
INSERT [dbo].[LeopardType] ([LeopardTypeId], [LeopardTypeName], [Origin], [Description]) VALUES (3, N'Indochinese Leopard', N'Bali', N'The Indochinese leopard (Panthera pardus delacouri) is native to southern China and mainland Southeast Asia')
SET IDENTITY_INSERT [dbo].[LeopardType] OFF
GO
ALTER TABLE [dbo].[LeopardProfile]  WITH CHECK ADD  CONSTRAINT [FK_LeopardProfile_LeopardType] FOREIGN KEY([LeopardTypeId])
REFERENCES [dbo].[LeopardType] ([LeopardTypeId])
GO
ALTER TABLE [dbo].[LeopardProfile] CHECK CONSTRAINT [FK_LeopardProfile_LeopardType]
GO