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
 @WakeTime DATETIME,
 @InstanceName nvarchar(100) = ''
AS
-- NB: WITH statements require a ';' on the statement immediately previous
BEGIN TRANSACTION;
-- Uses a CTE to allow ORDER BY WakeTime, and to throttle by @rows
-- (because you cannot ORDER BY an UPDATE statement)
WITH Results as
(
	SELECT TOP (@rows) ws.WorkItemID, ws.WakeTime 
	FROM [dbo].WorkItemStatus ws with (UPDLOCK)
	JOIN [dbo].WorkItems wi on wi.WorkItemId = ws.WorkItemId
	WHERE ws.Status = @status 
		and ws.Waketime <= @WakeTime
		and wi.InstanceName = @InstanceName
	ORDER BY ws.WakeTime ASC
)
-- Performs the UPDATE and OUTPUTs the INSERTED. fields to the calling app
UPDATE 
	[dbo].WorkItemStatus 
	SET Status = 2
OUTPUT 
	INSERTED.WorkItemID as WorkItemId, 
	2 as Status, 
	INSERTED.WakeTime as WakeTime, 
	wi.BindingKey as BindingKey, 
	wi.InnerMessage as InnerMessage,
	wi.CancellationKey as CancellationKey,
	wi.Exchange as Exchange,
	wi.ExchangeType as ExchangeType,
	wi.RoutingKey as RoutingKey,
	wi.MessageProperties as MessageProperties
FROM WorkItemStatus ws
	INNER JOIN Results r       -- this JOIN filters our UPDATE to the @rows SELECTed
ON r.WorkItemID = ws.WorkItemID
	INNER JOIN WorkItems wi    -- this JOIN is purely to allow OUTPUT of Bindingkey and InnerMessage
ON ws.WorkItemID = wi.WorkItemID

IF @@ERROR > 0 
	ROLLBACK TRANSACTION
ELSE
	COMMIT TRANSACTION
