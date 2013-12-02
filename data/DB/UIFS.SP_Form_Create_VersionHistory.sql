SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- =============================================
-- Author:		Joel Serbinski
-- Create date: 20100729
-- Description:	Creates a version history of a form and its controls
-- =============================================
CREATE PROCEDURE [dbo].[UIFS.SP_Form_Create_VersionHistory]
	-- Add the parameters for the stored procedure here
	@formid int = 0
AS
BEGIN
	SET NOCOUNT ON;

INSERT INTO [UIFS.FormVersionHistory] ([formid],[formversion],[controlid],[controlversion])

	SELECT Form.id,Form.currentversion,FCs.id,FCs.[version]  FROM [UIFS.Form] AS Form
	INNER JOIN [UIFS.FormControls] AS FCs ON Form.id = FCs.formid
	WHERE Form.id = @formid AND FCs.active=1

END

GO


