SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('[dbo].[uspGetNextBatchOfMessages]') AND type_desc IN ('SQL_STORED_PROCEDURE'))
BEGIN
	PRINT 'Dropping procedure [dbo].[uspGetNextBatchOfMessages]'
	DROP PROCEDURE [dbo].[uspGetNextBatchOfMessages]
END
GO

PRINT 'Creating procedure [dbo].[uspGetNextBatchOfMessages]'
GO

CREATE PROCEDURE [dbo].[uspGetNextBatchOfMessages]
 @rows INT = 1000, 
 @status TINYINT = 0,
 @WakeTime DATETIME

AS

-- NB: WITH statements require a ';' on the statement immediately previous
BEGIN TRANSACTION;


-- Uses a CTE to allow ORDER BY WakeTime, and to throttle by @rows
-- (because you cannot ORDER BY an UPDATE statement)
WITH Results as
(
SELECT TOP (@rows) WorkItemID, WakeTime 
FROM WorkItemStatus ws with (UPDLOCK)
WHERE ws.Status = @status and ws.Waketime <= @WakeTime
ORDER BY ws.WakeTime ASC
)
-- Performs the UPDATE and OUTPUTs the INSERTED. fields to the calling app
UPDATE WorkItemStatus
SET Status = 2
OUTPUT INSERTED.WorkItemID, 2 as Status, INSERTED.WakeTime, wi.BindingKey, wi.InnerMessage
FROM WorkItemStatus ws
INNER JOIN Results r       -- this JOIN filters our UPDATE to the @rows SELECTed
ON r.WorkItemID = ws.WorkItemID
INNER JOIN WorkItems wi    -- this JOIN is purely to allow OUTPUT of Bindingkey and InnerMessage
ON ws.WorkItemID = wi.WorkItemID

IF @@ERROR > 0 
	ROLLBACK TRANSACTION
ELSE
	COMMIT TRANSACTION
