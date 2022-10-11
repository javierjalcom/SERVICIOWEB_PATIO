-- spCRUDVisitContQueue

/*
                   
	DROP PROCEDURE spCRUDVisitContQueue
	          
    -- prcesos diversos para visicont

*/

    CREATE PROCEDURE spCRUDVisitContQueue @intMode int,  @intVisitId numeric(16) , @intContainerUniversalId numeric(18), @strYardPosition varchar(10), @strContainer varchar(20) ,@struser varchar(40)
    AS
       DECLARE @lint_MaxVisit numeric(16)
       DECLARE @lint_MaxQueue numeric(16)
       
       DECLARE @lintServiceId int
       DECLARE @SOrderId numeric(16)
       DECLARE @lintServiceType int
       DECLARE @SOrderDetailId numeric(16)
       DECLARE @ServQueuId numeric(16)
       DECLARE @strService varchar(10)
       DECLARE @ServiceId numeric(16)
       DECLARE @intCountPendConfirm INT
       DECLARE @UniversalId NUMERIC(16)
       DECLARE @intCountContOut INT
       DECLARE @iovalue INT
       DECLARE @StatusId  INT
       DECLARE @StatErrSP   INT
       DECLARE @strContName varchar(20)
      DECLARE @Comments  VARCHAR(100)


       -- si el modo es 1 , consultar la informacion 
       IF @intMode =1
         BEGIN
              -- si no tiene visita, obtener visita  
	           SELECT @lint_MaxVisit = MAX(tblclsVisitContainer.intVisitId)
	           FROM tblclsVisitContainer
	           WHERE tblclsVisitContainer.intContainerUniversalId = @intContainerUniversalId
	           
	           SET @lint_MaxVisit = ISNULL(@lint_MaxVisit,0)
	           SET @intVisitId = @lint_MaxVisit
	           
	           --si la visita es 0, salir
	           IF ( @lint_MaxVisit = 0)
	           BEGIN
	             RETURN 0
	           END 
      
		       --obtener el servicio 
		       SELECT  @strService = tblclsService.strServiceIdentifier
		              , @lintServiceType = tblclsVisitContainer.intServiceId
		              , @strService = tblclsService.strServiceIdentifier
		              , @SOrderId = tblclsVisitContainer.intServiceOrderId
		              , @ServiceId = tblclsVisitContainer.intServiceId
		              ,@strContName = tblclsVisitContainer.strContainerId
		       FROM tblclsVisitContainer
		        INNER JOIN tblclsService ON tblclsService.intServiceId = tblclsVisitContainer.intServiceId
		       WHERE tblclsVisitContainer.intVisitId = @intVisitId --@lint_MaxVisit
		       ------------------
		       
		       SELECT @strService AS 'SERVICE' 
		             , @lintServiceType AS 'IDSERVICETYPE'
		             , @SOrderId AS 'SO'
		             , @intVisitId AS 'VISITID'
		             , @strContName as 'STRCONTNAME'
		            RETURN 0
		              
		        -- obtener el id de la servicequeue
		       -- SELECT @ServQueuId =MAX(tblclsServiceQueu.intServiceQueuId)
		       -- FROM tblclsServiceQueu
		       -- WHERE tblclsServiceQueu.intVisitId = @intVisitId
		       -- AND tblclsServiceQueu.intContainerUniversalId = @intContainerUniversalId
		       --  AND tblclsServiceQueu.intServiceOrderId = @SOrderId
		       --   END  
		               ------@intMode ----
	  END -- fin del modo 1 
       
       --- si el modo es 1 , consultar la informacion 
       --IF @intMode =2
        -- BEGIN
        -- END
         
      -- si el modo es 1 , consultar la informacion 
      -- IF @intMode =3
      --  BEGIN
      --  END
           