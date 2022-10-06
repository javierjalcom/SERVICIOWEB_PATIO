-- spMarkProcessVisitSrvQueue

/*
                   
	DROP PROCEDURE spMarkProcVContainerQueue
	          
    -- marca procesado un contenedor con universal de una visita del servicio que le corresponde

*/

    CREATE PROCEDURE spMarkProcVContainerQueue @intVisitId numeric(16) , @intContainerUniversalId numeric(18), @strYardPosition varchar(10) ,@struser varchar(40)
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
      DECLARE @Comments  VARCHAR(100)

       SET @struser = @struser + '_m'
       SET @intVisitId = ISNULL(@intVisitId,0)
      
       -- si no tiene visita, obtener visita  
       IF (@intVisitId=0)
       BEGIN
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
       END 
      
       --obtener el servicio 
       SELECT  @strService = tblclsService.strServiceIdentifier
              , @lintServiceType = tblclsVisitContainer.intServiceId
              , @strService = tblclsService.strServiceIdentifier
              , @SOrderId = tblclsVisitContainer.intServiceOrderId
              , @ServiceId = tblclsVisitContainer.intServiceId
       FROM tblclsVisitContainer
        INNER JOIN tblclsService ON tblclsService.intServiceId = tblclsVisitContainer.intServiceId
       WHERE tblclsVisitContainer.intVisitId = @intVisitId --@lint_MaxVisit
       ------------------
        -- obtener el id de la servicequeue
        SELECT @ServQueuId =MAX(tblclsServiceQueu.intServiceQueuId)
        FROM tblclsServiceQueu
        WHERE tblclsServiceQueu.intVisitId = @intVisitId
        AND tblclsServiceQueu.intContainerUniversalId = @intContainerUniversalId
        AND tblclsServiceQueu.intServiceOrderId = @SOrderId
        ---
        -- obtener el estatus terminado
        
        SELECT @StatusId =  tblclsServiceOrderStatus.intSOStatusId
        FROM tblclsServiceOrderStatus
        WHERE tblclsServiceOrderStatus.strSOStatusIdentifier ='TER'
        --------------------
        
        --- marcar la servicequere
            
        BEGIN TRAN

        UPDATE tblclsServiceQueu
           SET intSOStatusId              = @StatusId  ,
               --dtmServiceQueuStartDate    = @dtmStart    ,
               blnServiceQueuExecuted     = 1 ,
               dtmServiceQueuExecDate     = getdate() ,
               dtmServiceQueuLastModified = getDate(),
               strServiceQueuModifiedBy   = @struser 
         WHERE  intServiceQueuId        =  @ServQueuId AND
               intServiceId            =  @ServiceId      AND               
              intServiceOrderId       =  @SOrderId       AND                              
              tblclsServiceQueu.intContainerUniversalId = @intContainerUniversalId
               
         COMMIT TRAN

        -------
        

        -- si es entrega 
            IF @strService = 'ENTLL' OR @strService = 'ENTV'
             BEGIN
			        --Determina elUnivesaL Id
			        SELECT @UniversalId = intContainerUniversalId      
			          FROM tblclsServiceQueu
			         WHERE blnServiceQueuExecuted         =1 AND
			               intServiceQueuId        =  @ServQueuId   
			

			
			        --Si se recupero el Universal Id del Contenedor marcado como completado
			        IF @UniversalId>0 
			        BEGIN
			            -- MODIFICACIÓN BLOERA   Agregar al histórico la Descarga
			            SELECT @Comments = 'Carga de Contenedor registrada por ' + @struser
			
			            EXECUTE  @StatErrSP   = spUpdateHistoryOtherData @UniversalId, 27, @Comments,  @struser
			        END
			
			      
			        -->>Checar si la Visita del Registro Actual tienen CONTENDORES DE SALIDA ( ENTV/ENTLL) PENDIENTES DE ACTUALIZAR        
			        SELECT @intCountContOut = COUNT(*)  FROM tblclsServiceQueu SQ 
			         WHERE SQ.intVisitId   = ( SELECT  SQ2.intVisitId 
			                                      FROM     tblclsServiceQueu SQ2 
			                                     WHERE SQ2.intServiceQueuId = @ServQueuId  ) AND
			               SQ.intServiceId IN ( SELECT S.intServiceId 
			                                      FROM tblclsService S 
			                                     WHERE S.strServiceIdentifier  IN ('ENTV','ENTLL') 
			                                   ) 
			           AND SQ.intSOStatusId   = NULL
			
			
			        -->>SI TIENE CONT DE SALIDA PENDIENTES DE ACTUALIZA Y YA NO HAYA CONTENDORES PENDIENTE CONFIRMAR POSICION
			        IF @intCountContOut>0 AND  @intCountPendConfirm =1
			        BEGIN
			            --select 'SI TIENE CONT DE SALIDA PENDIENTES DE ACTUALIZA Y YA NO HAYA CONTENDORES PENDIENTE CONFIRMAR POSICION'
			
			            BEGIN TRAN
			
			            --Evitar Dirty Reads
			            SET TRANSACTION ISOLATION LEVEL 1 	
			
			            
			            --Actualizar la Fecha de Inicio de Ejecución en Maniobras de Salida(ENTV/ENTLL)
			            UPDATE tblclsServiceQueu 
			               SET dtmServiceQueuStartDate    = getDate()    ,               
			                   dtmServiceQueuLastModified = getDate(),
			                   strServiceQueuModifiedBy   = @struser
			            
			             WHERE intVisitId = ( SELECT  SQ2.intVisitId 
			                                    FROM     tblclsServiceQueu SQ2 
			                                   WHERE SQ2.intServiceQueuId = @ServQueuId  ) AND
			                   intServiceId IN ( SELECT S.intServiceId 
			                                     FROM tblclsService S 
			                                    WHERE S.strServiceIdentifier  IN ('ENTV','ENTLL') 
			                                  )  
			               AND intSOStatusId   = NULL
			            
			            COMMIT TRAN
			            /*select 'aplicat act'*/
			            --SELECT @ErrorCode=8
			            
			            RETURN 0 
			        END -- cont out         

             
             END -- si es entrega             IF @strService = 'ENTLL' OR @strService = 'ENTV'

             
        -- si es recepcion 
        IF @strService = 'RECLL' OR @strService = 'RECV' OR @strService = 'RECVOS' 
            BEGIN
            
                --Evitar Dirty Reads
                SET TRANSACTION ISOLATION LEVEL 1

		        -->>CHECAR CUANTOS CONTENEDORES PENDIENTES DE CONFIRMAR HAY(COMPLETADO=0 Y FIN DE EJECUCION=0)
		        SELECT @intCountPendConfirm  = COUNT(*)  FROM tblclsServiceQueu SQ 
		        WHERE  SQ.intVisitId   = ( SELECT  SQ2.intVisitId 
		                                   FROM     tblclsServiceQueu SQ2 
		                                   WHERE SQ2.intServiceQueuId =   @ServQueuId    ) AND
		               SQ.intServiceId IN ( SELECT S.intServiceId 
		                                    FROM tblclsService S 
		                                    WHERE S.strServiceIdentifier  IN ('RECV','RECVOS','RECLL') 
		                                  ) AND SQ.intSOStatusId   = NULL AND
		               SQ.blnServiceQueuExecuted =0 AND 
		               SQ.dtmServiceQueuExecDate=NULL      
		               
		        /*select 'cantidad de contenedores de descarga pendientes '
            		SELECT @intCountPendConfirm */
            		
               --Determina elUnivesaL Id por Cargar/Descargar BLOERA 28/OCT/2005
               SELECT @UniversalId = intContainerUniversalId      
               FROM tblclsServiceQueu
               WHERE blnServiceQueuExecuted         =1 AND
               intServiceQueuId        =  @ServQueuId   

              --Si se recupero el Universal Id del Contenedor marcado como completado
              -- generar comnetarios         
              IF (@UniversalId>0 )
                  BEGIN
                        SELECT @Comments = 'Descarga de Contenedor registrada por ' +   @struser
                        EXECUTE  @StatErrSP  = spUpdateHistoryOtherData @UniversalId, 27, @Comments,  @struser
                 END
                 
                 
                --actualizar maniobras asociadas al contenedor 
                  EXECUTE @StatErrSP  = spUpdateSOContainerWhenIngress @intContainerUniversalId , @struser , @iovalue 
                  
              -- IF @StatErrSP =1 or @iovalue=1
				--- BEGIN
				--	 RAISERROR 99999 'Error al actualizar maniobra '
				--	 RETURN 1
                -- END

            END --IF @strService = 'RECLL' OR @strService = 'RECV' OR @strService = 'RECVOS' 
        --- fin si es recepcion 
      -------------------------------------------------
        
-------------------------------      