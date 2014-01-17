SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('[dbo].[uspMarkWorkItemForPurge]') AND type_desc IN ('SQL_STORED_PROCEDURE'))
BEGIN
	PRINT 'Dropping procedure [dbo].[uspMarkWorkItemForPurge]'
	DROP PROCEDURE [dbo].[uspMarkWorkItemForPurge]
END
GO

PRINT 'Creating procedure [dbo].[uspMarkWorkItemForPurge]'
GO

CREATE PROCEDURE [dbo].[uspMarkWorkItemForPurge]
 @ID INT = 0, @purgeDate datetime = NULL

AS

-- Performs the UPDATE and OUTPUTs the INSERTED. fields to the calling app
UPDATE WorkItemStatus
SET PurgeDate = @purgeDate
OUTPUT INSERTED.WorkItemID, INSERTED.purgeDate
FROM WorkItemStatus ws
WHERE WorkItemID = @ID
