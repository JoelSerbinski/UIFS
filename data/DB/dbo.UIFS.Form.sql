SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

SET ANSI_PADDING ON
GO

CREATE TABLE [dbo].[UIFS.Form](
	[id] [int] IDENTITY(1,1) NOT NULL,
	[currentversion] [smallint] NOT NULL,
	[name] [varchar](255) NOT NULL,
	[description] [varchar](max) NOT NULL,
	[active] [bit] NOT NULL,
	[created] [datetime] NOT NULL,
	[createdby] [varchar](255) NOT NULL,
	[lastmodified] [datetime] NULL,
	[lastmodifiedby] [varchar](255) NULL,
 CONSTRAINT [PK_Form] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]

GO

SET ANSI_PADDING OFF
GO

ALTER TABLE [dbo].[UIFS.Form] ADD  CONSTRAINT [DF_Form_active]  DEFAULT ((1)) FOR [active]
GO

ALTER TABLE [dbo].[UIFS.Form] ADD  CONSTRAINT [DF_Form_created]  DEFAULT (getdate()) FOR [created]
GO


