SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- =============================================
-- Author:		Joel Serbinski
-- Create date: 20120327
-- Description:	We can have End values that are after midnight and actually calculate a negative value
--				because End time is then before Start time.  So, for these, we just flip it around and
--				calculate against the daily value of minutes! 24hrs * 60min = 1440 min
-- =============================================
CREATE FUNCTION [dbo].[CalcTimeDiff]
(@Start DATETIME, @End DATETIME)
RETURNS int
AS
BEGIN
	DECLARE @MinDiff INT
	SET @MinDiff = DATEDIFF(mi, @Start, @End)
	IF @MinDiff < 0 SET @MinDiff=(1440)+@MinDiff
	RETURN @MinDiff
END

GO


