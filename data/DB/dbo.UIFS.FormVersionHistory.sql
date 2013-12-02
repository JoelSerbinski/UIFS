SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[UIFS.FormVersionHistory](
	[formid] [int] NOT NULL,
	[formversion] [int] NOT NULL,
	[controlid] [int] NOT NULL,
	[controlversion] [int] NOT NULL,
 CONSTRAINT [PK_UIFS.FormVersionHistory] PRIMARY KEY CLUSTERED 
(
	[formid] ASC,
	[formversion] ASC,
	[controlid] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]

GO

ALTER TABLE [dbo].[UIFS.FormVersionHistory]  WITH CHECK ADD  CONSTRAINT [FK_UIFS.FormVersionHistory_UIFS.Form] FOREIGN KEY([formid])
REFERENCES [dbo].[UIFS.Form] ([id])
ON DELETE CASCADE
GO

ALTER TABLE [dbo].[UIFS.FormVersionHistory] CHECK CONSTRAINT [FK_UIFS.FormVersionHistory_UIFS.Form]
GO


