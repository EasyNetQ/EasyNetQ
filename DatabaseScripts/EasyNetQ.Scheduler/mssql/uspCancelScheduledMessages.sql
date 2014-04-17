SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID('[dbo].[uspCancelScheduledMessages]') AND type_desc IN ('SQL_STORED_PROCEDURE'))
BEGIN
	PRINT 'Dropping procedure [dbo].[uspCancelScheduledMessages]'
	DROP PROCEDURE [dbo].[uspCancelScheduledMessages]
END
GO

PRINT 'Creating procedure [dbo].[uspCancelScheduledMessages]'
GO

CREATE PROCEDURE [dbo].[uspCancelScheduledMessages] 
	@CancellationKey NVARCHAR(255)
AS

DELETE FROM WorkItems WHERE CancellationKey = @CancellationKey