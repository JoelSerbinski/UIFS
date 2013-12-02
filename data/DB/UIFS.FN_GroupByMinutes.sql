SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE FUNCTION [dbo].[GroupByMinutes] (
    @SourceDateTime DATETIME,
    @MinutesPerBucket INT
)
RETURNS DATETIME
AS
BEGIN
	/* 
	
	-- MY TESTING to achieve this func
	DECLARE @IntervalMinutes INT
	SET @IntervalMinutes = 15

	SELECT DATEADD(mi,(DATEDIFF(mi,'19000101',submittedon) / @IntervalMinutes) * @IntervalMinutes,0), COUNT(*)
	FROM [Form] GROUP BY DATEADD(mi,(DATEDIFF(mi,'19000101',submittedon) / @IntervalMinutes) * @IntervalMinutes,0)
	ORDER BY DATEADD(mi,(DATEDIFF(mi,'19000101',submittedon) / @IntervalMinutes) * @IntervalMinutes,0) desc

	-- REF: http://ryancoder.blogspot.com/2010/06/bucket-datetimes-in-t-sql.html

	*/
    IF (@MinutesPerBucket < 1) RETURN NULL
    -- Epoch is 1/1/1753 for the DATETIME type that is an 8byte value with milliseconds...range is 1/1/1753-12/31/9999
    DECLARE @Epoch DATETIME
    SET @Epoch = CAST('17530101' AS DATETIME)
    DECLARE @MinutesSinceEpoch INT                
    DECLARE @BucketMinutesSinceEpoch INT    
    
    SET @MinutesSinceEpoch = DATEDIFF(mi, @Epoch, @SourceDateTime)    
    SET @BucketMinutesSinceEpoch = @MinutesSinceEpoch - (@MinutesSinceEpoch % @MinutesPerBucket)    
    RETURN DATEADD(mi, @BucketMinutesSinceEpoch, @Epoch)
    
END


GO


