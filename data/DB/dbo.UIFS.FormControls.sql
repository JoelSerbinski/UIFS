SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

SET ANSI_PADDING ON
GO

CREATE TABLE [dbo].[UIFS.FormControls](
	[formid] [int] NOT NULL,
	[id] [smallint] NOT NULL,
	[version] [smallint] NOT NULL,
	[active] [bit] NOT NULL,
	[type] [int] NOT NULL,
	[name] [varchar](255) NOT NULL,
	[prompt] [varchar](255) NOT NULL,
	[tip] [varchar](max) NULL,
	[ordernum] [smallint] NOT NULL,
	[required] [bit] NOT NULL,
	[textbox_lines] [int] NULL,
	[textbox_width] [int] NULL,
	[textbox_full] [bit] NULL,
	[list_options] [varchar](max) NULL,
	[list_type] [tinyint] NULL,
	[checkbox_type] [tinyint] NULL,
	[checkbox_initialstate] [bit] NULL,
	[checkbox_hasinput] [bit] NULL,
	[datetime_type] [tinyint] NULL,
	[number_min] [numeric](19, 4) NULL,
	[number_max] [numeric](19, 4) NULL,
	[number_interval] [numeric](19, 4) NULL,
	[number_slider] [bit] NULL,
	[percentage_interval] [int] NULL,
	[range_type] [tinyint] NULL,
	[range_min] [numeric](19, 4) NULL,
	[range_max] [numeric](19, 4) NULL,
 CONSTRAINT [PK_Form_Controls_1] PRIMARY KEY CLUSTERED 
(
	[formid] ASC,
	[version] ASC,
	[id] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]

GO

SET ANSI_PADDING OFF
GO

ALTER TABLE [dbo].[UIFS.FormControls]  WITH CHECK ADD  CONSTRAINT [FK_Form_Controls_Form] FOREIGN KEY([formid])
REFERENCES [dbo].[UIFS.Form] ([id])
ON DELETE CASCADE
GO

ALTER TABLE [dbo].[UIFS.FormControls] CHECK CONSTRAINT [FK_Form_Controls_Form]
GO

ALTER TABLE [dbo].[UIFS.FormControls] ADD  CONSTRAINT [DF_UIFS.FormControls_active]  DEFAULT ((1)) FOR [active]
GO

ALTER TABLE [dbo].[UIFS.FormControls] ADD  CONSTRAINT [DF_UIFS.FormControls_required]  DEFAULT ((1)) FOR [required]
GO


