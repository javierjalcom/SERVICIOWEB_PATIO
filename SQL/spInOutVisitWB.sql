/*
Highlight and execute the following statement to drop the procedure
before executing the create statement.

DROP PROCEDURE spInOutVisitWB

*/

CREATE PROCEDURE spInOutVisitWB  @intVisitId          udtIdentifier,  
                                  -- @dtmReceptionDate    DATETIME,  
                                   @strService          udtStringIdentifier,  
                                   @strUser             udtUserName         
  
AS  
  
DECLARE @strContainerId VARCHAR(12)  
DECLARE @StatErrSP      INTEGER  
DECLARE @intVisitstatus INTEGER 
DECLARE @intEmptyRowsCount INTEGER  
DECLARE @intGeneralCargoUniversalId udtIdentifier 
DECLARE @intGCInventoryItemId udtIdentifier 
declare @intServiceOrderId udtIdentifier 
DECLARE @intServiceOrderDetailId udtInteger 
DECLARE @intServiceId    udtInteger 
DECLARE @intVisitGCQuantity udtInteger 
DECLARE @decVisitGCWeight  udtDecimal 
declare @OutGCUniversal udtIdentifier 
DECLARE @ldtmReceptionDate datetime
 
 
DECLARE @intCountEmptyCont INTEGER 
DECLARE @intCountCont      INTEGER  
DECLARE @intvStatus        INTEGER
 
 
SELECT  @intVisitstatus = intSOStatusId         
FROM    tblclsServiceOrderStatus 
WHERE   strSOStatusIdentifier = 'TER'  
 
 
SELECT @intServiceId = intServiceId 
FROM tblclsService 
WHERE  strServiceIdentifier = 'ENTCG' 
 
 --- estatus de la visita 
 SELECT @intvStatus = tblclsVisit.intVisitId
 FROM tblclsVisit
 WHERE intVisitId = @intVisitId
 ----
--- counter so
  DECLARE @lint_CountSO udtIdentifier 
  
    SELECT @lint_CountSO =  tblclsVisitServiceOrder.intVisitId
    FROM tblclsVisitServiceOrder
    WHERE intVisitId = @intVisitId
---------
---  fecha de operacion de visita
  set @ldtmReceptionDate = GETDATE()
 
IF  @strService LIKE 'ENT%'  
BEGIN 
      --- 03-nov-2015 , prueba temporal 
      IF @lint_CountSO > 0 
       BEGIN        
         UPDATE tblclsVisit 
         SET tblclsVisit.strVisitComments = tblclsVisit.strVisitComments + '-E1'
         WHERE tblclsVisit.intVisitId = @intVisitId 
         
       END 
 
       -- ACTUALIZACION DE status 
       IF (@intvStatus <=1 )
       BEGIN
           UPDATE tblclsVisit 
         SET tblclsVisit.intSOStatusId = 2
         WHERE tblclsVisit.intVisitId = @intVisitId 
       
       END -- ACTUALIZACION DE status 
       
    IF EXISTS (select  intVisitId  from tblclsVisitGeneralCargo 
                   where tblclsVisitGeneralCargo.intVisitId = @intVisitId  
                   and tblclsVisitGeneralCargo.intServiceId = @intServiceId ) 
    BEGIN -- Si el servico es checkout de Entrega de CG 
 
        /****************************************************************************  
         Crea el cursor que recorre ITEMS de carga de la visita, para la salida de GC  
         ****************************************************************************/  
  
        DECLARE GCCursor CURSOR  
        FOR select tblclsVisitGeneralCargo.intGeneralCargoUniversalId,    
                   tblclsVisitGeneralCargo.intGCInventoryItemId , 
                   tblclsVisitGeneralCargo.intServiceOrderId,    
                   tblclsVisitGeneralCargo.intServiceOrderDetailId,                
                   tblclsVisitGeneralCargo.intVisitGCQuantity,    
                   tblclsVisitGeneralCargo.decVisitGCWeight 
              FROM tblclsVisitGeneralCargo ,    
                   tblclsGeneralCargoInventory ,    
                   tblclsService   ,
                   tblclsGCInventoryItem                   
           WHERE --( tblclsVisitGeneralCargo.intGeneralCargoUniversalId *= tblclsGeneralCargoInventory.intGeneralCargoUniversalId) and   
	             ( tblclsVisitGeneralCargo.intGeneralCargoUniversalId = tblclsGeneralCargoInventory.intGeneralCargoUniversalId) and   
                 ( tblclsVisitGeneralCargo.intServiceId = tblclsService.intServiceId ) and   
                 ( tblclsVisitGeneralCargo.intVisitId = @intVisitId )  and
                 ( tblclsVisitGeneralCargo.intGeneralCargoUniversalId *= tblclsGCInventoryItem.intGeneralCargoUniversalId) and 
                 ( tblclsVisitGeneralCargo.intGCInventoryItemId *= tblclsGCInventoryItem.intGCInventoryItemId ) and 
                 ( tblclsGCInventoryItem.blnGCInvItemActive = 1 )
  
  
        OPEN  GCCursor  
  
        --Obtiene la Informacion del Select  
        FETCH GCCursor INTO @intGeneralCargoUniversalId,@intGCInventoryItemId,@intServiceOrderId,@intServiceOrderDetailId,@intVisitGCQuantity,@decVisitGCWeight 
  
        WHILE @@sqlstatus = 0  
        BEGIN  
           SELECT 'ENTRA A LA ENTCG' 
           IF @intGeneralCargoUniversalId IS NULL or @intGeneralCargoUniversalId=0 
           BEGIN 
             
               RAISERROR 99999 'No se puede dar salida a una Visita sin mercancia(s) ' 
               RETURN 1 --ERROR : GCuniversal no existe en el inverario  
    
           END 
           --execute dbo.spGCInOutInventory @ServiceType, @GCUnivId,            @VisitId,    @ServiceOrderId,   @ServiceOrderItemId, 
           --@FisMovId,@ProductId,@ProductPackingId,@GCTypeId,@GCDamTypeId, @WarehouseId, @CustomerTypeId, @CustomerId, @IsOutDoor,  
           --@Qty,  
           --@Marks, @Numbers,  
           --@Weight,  
           --@Volume, @CommValue, @PositionId, @VessVoyId, @OriginPort, @DischargePort, @FinalPort, @Comments,  
           --@User, @GCItemId, @OutGCUniversal out,  
           --@ReqById, @ReqByTypeId 
            
           EXECUTE @StatErrSP = spGCInOutInventory 'ENTCG',@intGeneralCargoUniversalId,@intVisitId,@intServiceOrderId,@intServiceOrderDetailId,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,@intVisitGCQuantity,NULL,NULL,@decVisitGCWeight,NULL,NULL,NULL,NULL,NULL,NULL,NULL,NULL,@strUser,@intGCInventoryItemId,@OutGCUniversal out,NULL,NULL  
            
           IF @StatErrSP  <> 0 --Validacion del SP  
           BEGIN  
           	 
      	       SELECT 'ERROR: Al ACTUALIZAR el Inventario de GC' 
               RETURN (@StatErrSP) --ERROR: Al ACTUALIZAR el Inventario de GC 
           END  
  
        --Obtiene la Informacion del Select  
        FETCH GCCursor INTO @intGeneralCargoUniversalId,@intGCInventoryItemId,@intServiceOrderId,@intServiceOrderDetailId,@intVisitGCQuantity,@decVisitGCWeight  
        END --end while 
    END --end salida de cg 
     
    ELSE 
    BEGIN 
       SELECT  @intCountEmptyCont = COUNT(strContainerId)   
       FROM    tblclsVisitContainer, tblclsService    
       WHERE   intVisitId                = @intVisitId  
           AND tblclsVisitContainer.intServiceId  = tblclsService .intServiceId    
           AND tblclsVisitContainer.blnVisitContainerIsCancelled  = 0  
           AND ISNULL(tblclsVisitContainer.strContainerId,'') = '' 
           AND tblclsService.strServiceIdentifier  LIKE @strService +'%'  
     
       SELECT  @intCountCont = COUNT(strContainerId)   
       FROM    tblclsVisitContainer, tblclsService    
       WHERE   intVisitId                = @intVisitId  
           AND tblclsVisitContainer.intServiceId  = tblclsService .intServiceId    
           AND tblclsVisitContainer.blnVisitContainerIsCancelled  = 0  
           AND tblclsService.strServiceIdentifier  LIKE @strService +'%'  
     
     
       IF @intCountEmptyCont > 0 AND @intCountEmptyCont <  @intCountCont 
        BEGIN 
                 
            RAISERROR 99999 'Falta(n) %1! Contenedor(s) por Asignar a la Visita', @intCountEmptyCont 
            RETURN 1 --ERROR :  
        
        END 
         
       IF @intCountEmptyCont <> 0 AND @intCountEmptyCont = @intCountCont  
        BEGIN 
            /****************************************************************************  
            Actualiza la fecha de salida y el usuario  de la visita de salida  
            ****************************************************************************/  
             
            BEGIN TRAN tblclsVisit  
            UPDATE  tblclsVisit  
            SET  dtmVisitDatetimeIn      =  Null,  
                --intSOStatusId           =  @intVisitstatus,  
                dtmVisitLastModified    = getDate(),  
                strVisitLastModifiedBy  = @strUser, 
                strVisitComments =strVisitComments +', La visita Ingreso el ' + convert(varchar(12),dtmVisitDatetimeIn,105) + ' a las ' + convert(varchar(12),dtmVisitDatetimeIn,108)  
            WHERE  intVisitId   = @intVisitId   
              
            IF @@ERROR <> 0   
            BEGIN            
              SELECT 'No se pudo modificar la Visita' + @strContainerId  
              ROLLBACK TRAN  tblclsVisit  
              RETURN 1  
            END  
            COMMIT TRAN tblclsVisit  
            /****************************************************************************  
            Elimina la visita de la cola de camiones  
            ****************************************************************************/  
             
            DELETE FROM tblclsVisitQueu  
            WHERE intVisitId = @intVisitId     
                  
             --- 03-nov-2015 , prueba temporal 
            IF @lint_CountSO > 0  
            BEGIN             
			  UPDATE tblclsVisit
			  SET tblclsVisit.strVisitComments = tblclsVisit.strVisitComments + '-F1'
			  WHERE tblclsVisit.intVisitId = @intVisitId 
			  
			END 
             
             
            IF @@Error = 1  --Validacion al Borrar el Registro  
            BEGIN  
            ROLLBACK TRAN    --Aborta los Cambios  
            RETURN 1         --ERROR: Al Insertar la Visita en la Cola de Camiones  
            END  
            ELSE COMMIT TRAN    --Aplica los Cambios  
            -->>Inserta itesm de la Visita en la Cola de Servicios para Maniobras por Puerta  
            EXECUTE @StatErrSP = spServiceQueuInOut 0, @intVisitId, @strUser  
                 
            RAISERROR 99999 'Se Cancelo el Check In por que la visita no ejecuto el Servicio' 
            RETURN 1 --ERROR : Contenedor no Existe en el Inventario   
        
        END 
         
    END 
END 
  
 
/****************************************************************************  
Crea el cursor que recorre los contenedores de la visita, segun el servio  
****************************************************************************/  
  
DECLARE VisitCursor CURSOR  
FOR SELECT  strContainerId   
    FROM    tblclsVisitContainer, tblclsService    
    WHERE   intVisitId                = @intVisitId  
            AND tblclsVisitContainer.intServiceId  = tblclsService .intServiceId    
            AND tblclsVisitContainer.blnVisitContainerIsCancelled  = 0  
            AND tblclsService.strServiceIdentifier  LIKE @strService +'%'  
  
/*SELECT strContainerId FROM tblclsVisitContainer  
    WHERE intVisitId = @intVisitId*/  
  
OPEN  VisitCursor  
  
 --Obtiene la Informacion del Select  
FETCH VisitCursor INTO @strContainerId  
  
WHILE @@sqlstatus = 0  
BEGIN  
    IF @strContainerId = '' 
    BEGIN 
             
        RAISERROR 99999 'No se puede dar salida a una Visita sin Contenedor(s) ' 
        RETURN 1 --ERROR : Contenedor no Existe en el Inventario   
    
    END 
    EXECUTE @StatErrSP = spContainerInOutInventory @intVisitId, @strContainerId, @ldtmReceptionDate ,@strUser  
     IF @StatErrSP  = 1 --Validacion del SP  
        BEGIN  
          RETURN (1) --ERROR: Al Insertar en el Inventario    
        END  
  
    --Obtiene la Informacion del Select  
    FETCH VisitCursor INTO @strContainerId  
END  
  
  
  
IF  @strService LIKE 'REC%'  
BEGIN  
/****************************************************************************  
Actualiza la fecha de ingreso y el usuario de la visita  
****************************************************************************/  
--- 03-nov-2015 , prueba temporal 
     IF @lint_CountSO > 0  
         BEGIN 
              UPDATE tblclsVisit
              SET tblclsVisit.strVisitComments = tblclsVisit.strVisitComments + '-R1'
              WHERE tblclsVisit.intVisitId = @intVisitId 
         END

  
    BEGIN TRAN tblclsVisit  
    
    UPDATE  tblclsVisit  
    SET dtmVisitDatetimeIn      = GETDATE(),  
        dtmVisitLastModified    = getDate(),  
        strVisitLastModifiedBy  = @strUser  
    WHERE  intVisitId   = @intVisitId   
      
       -- ACTUALIZACION DE status 
       IF (@intvStatus <=1 )
       BEGIN
           UPDATE tblclsVisit 
         SET tblclsVisit.intSOStatusId = 2
         WHERE tblclsVisit.intVisitId = @intVisitId 
       
       END -- ACTUALIZACION DE status 
      
    IF @@ERROR <> 0   
    BEGIN            
      SELECT 'No se pudo modificar la Visita' + @strContainerId  
      ROLLBACK TRAN  tblclsVisit  
      RETURN 1  
    END  
    COMMIT TRAN tblclsVisit  
/****************************************************************************  
Inserta los datos en la cola de camiones  
****************************************************************************/  
 --Valida que la Visita Exista en la Cola de Visitas  
    IF NOT EXISTS (SELECT * FROM tblclsVisitQueu WHERE intVisitId=@intVisitId )  
    BEGIN  
        BEGIN TRAN  
        INSERT INTO tblclsVisitQueu  
                    (intVisitId,dtmVisitQueuCreationStamp,strVisitQueuCreatedBy,  
                    dtmVisitQueuLastModified,strVisitQueuLastModifiedBy )  
             VALUES (@intVisitId, getDate(),@strUser , getDate(),@strUser)  
            
         
          
        IF @@Error = 1  --Validacion al Insertar el Registro  
        BEGIN  
          ROLLBACK TRAN    --Aborta los Cambios  
          RETURN 1  --ERROR: Al Insertar la Visita en la Cola de Camiones  
        END  
        ELSE COMMIT TRAN    --Aplica los Cambios  
      
        -->>Inserta items de la Visita en la Cola de Servicios para Maniobras por Puerta  
        EXECUTE @StatErrSP =  spServiceQueuInOut 1, @intVisitId, @strUser  
    END   
  
END  
ELSE  
BEGIN  
/****************************************************************************  
Actualiza la fecha de salida y el usuario  de la visita de salida  
****************************************************************************/  
  
  ---- prueba temporal 03-nov-2015
      IF @lint_CountSO > 0  
         BEGIN 
		     UPDATE tblclsVisit
		     SET tblclsVisit.strVisitComments = tblclsVisit.strVisitComments + '-H1'
		     WHERE tblclsVisit.intVisitId = @intVisitId 
		 end
		     
  
    BEGIN TRAN tblclsVisit  
    UPDATE  tblclsVisit  
    SET dtmVisitDatetimeOut      = GETDATE(),
        dtmVisitDatetimeIn		= isnull(dtmVisitDatetimeIn,GETDATE()),  
        intSOStatusId           =  @intVisitstatus,  
        dtmVisitLastModified    = getDate(),  
        strVisitLastModifiedBy  = @strUser  
    WHERE  intVisitId   = @intVisitId   
      
    IF @@ERROR <> 0   
    BEGIN            
      SELECT 'No se pudo modificar la Visita' + @strContainerId  
      ROLLBACK TRAN  tblclsVisit  
      RETURN 1  
    END  
    COMMIT TRAN tblclsVisit  
  
/****************************************************************************  
Elimina la visita de la cola de camiones  
****************************************************************************/  
  
    DELETE FROM tblclsVisitQueu  
    WHERE intVisitId = @intVisitId           

             
    --- 03-nov-2015 , prueba temporal 
     IF @lint_CountSO > 0  
        BEGIN 
			UPDATE tblclsVisit
			SET tblclsVisit.strVisitComments = tblclsVisit.strVisitComments + '-X1'
			WHERE tblclsVisit.intVisitId = @intVisitId 
		END
   
   
    IF @@Error = 1  --Validacion al Borrar el Registro  
    BEGIN  
    ROLLBACK TRAN    --Aborta los Cambios  
    RETURN 1         --ERROR: Al Insertar la Visita en la Cola de Camiones  
    END  
    ELSE COMMIT TRAN    --Aplica los Cambios  
    -->>Inserta itesm de la Visita en la Cola de Servicios para Maniobras por Puerta  
    EXECUTE @StatErrSP = spServiceQueuInOut 0, @intVisitId, @strUser      
END  
RETURN 0









