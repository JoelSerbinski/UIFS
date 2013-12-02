SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- =============================================
-- Author:		Joel Serbinski
-- Create date: 20110207
-- Description:	Application needs to know the next available control id 
-- =============================================
CREATE PROCEDURE [dbo].[UIFS.SP_Form_GetNextAvailableControlID]
	@formid INT
AS
BEGIN
	SET NOCOUNT ON;
	SELECT MAX(id)+1 FROM [UIFS.FormControls] WHERE formid=@formid
END

GO


