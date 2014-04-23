SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('[dbo].[uspAddNewMessageToScheduler]') AND type_desc IN ('SQL_STORED_PROCEDURE'))
BEGIN
	PRINT 'Dropping procedure [dbo].[uspAddNewMessageToScheduler]'
	DROP PROCEDURE [dbo].[uspAddNewMessageToScheduler]
END
GO

PRINT 'Creating procedure [dbo].[uspAddNewMessageToScheduler]'
GO

CREATE PROCEDURE [dbo].[uspAddNewMessageToScheduler] 
	@WakeTime DATETIME,
	@BindingKey NVARCHAR(1000),
	@CancellationKey NVARCHAR(255) = NULL,
	@Message VARBINARY(MAX)
AS

DECLARE @NewID INT

BEGIN TRANSACTION

INSERT INTO WorkItems (BindingKey, CancellationKey, InnerMessage)
VALUES (@BindingKey, @CancellationKey, @Message)
-- get the ID of the inserted record for use in the child table
SELECT @NewID = SCOPE_IDENTITY()
IF @@ERROR > 0
	ROLLBACK TRANSACTION
ELSE
	-- only setup the child status record if the WorkItem insert succeeded
	BEGIN
		INSERT INTO WorkItemStatus (WorkItemID, [Status], WakeTime)
		OUTPUT INSERTED.WorkItemID, INSERTED.status, INSERTED.WakeTime
		VALUES (@NewID, 0, @WakeTime)
		
		IF @@ERROR > 0 
			ROLLBACK TRANSACTION
		ELSE
			BEGIN
				 COMMIT TRANSACTION
			END 
	END
