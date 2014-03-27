SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('[dbo].[uspWorkItemsSelfPurge]') AND type_desc IN ('SQL_STORED_PROCEDURE'))
BEGIN
	PRINT 'Dropping procedure [dbo].[uspWorkItemsSelfPurge]'
	DROP PROCEDURE [dbo].[uspWorkItemsSelfPurge]
END
GO

PRINT 'Creating procedure [dbo].[uspWorkItemsSelfPurge]'
GO

CREATE Procedure [dbo].[uspWorkItemsSelfPurge] @rows SmallINT = 5, @purgeDate DateTime = NULL 

AS

-- Only execute if there is work to do and continue 
-- until all records with a PurgeDate <= now are deleted
WHILE EXISTS(SELECT * FROM WorkItemStatus WHERE PurgeDate <= @purgeDate) 
BEGIN
      -- NB:  the FK in WorkItemStatus has ON DELETE CASCADE,
      -- so it will delete corresponding rows automatically
      DELETE TOP (@rows) WorkItems
      FROM WorkItems wi
      INNER JOIN WorkItemStatus ws
      ON wi.WorkItemID = ws.WorkItemID
      WHERE ws.PurgeDate <= @purgeDate
END -- WHILE EXISTS()

