SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

SET ANSI_PADDING ON
GO

CREATE TABLE [dbo].[UIFS.Log](
	[code] [int] NOT NULL,
	[date] [datetime] NOT NULL,
	[msg] [varchar](max) NOT NULL,
	[username] [varchar](255) NOT NULL,
	[exMessage] [varchar](max) NULL,
	[exSource] [varchar](max) NULL,
	[exStackTrace] [varchar](max) NULL,
	[CodeLocation] [varchar](max) NULL
) ON [PRIMARY]

GO

SET ANSI_PADDING OFF
GO

ALTER TABLE [dbo].[UIFS.Log] ADD  CONSTRAINT [DF_UIFS.Log_date]  DEFAULT (getdate()) FOR [date]
GO


