/*
Highlight and execute the following statement to drop the procedure
before executing the create statement.

DROP PROCEDURE dbo.spVisitContAsig

*/

CREATE PROCEDURE dbo.spVisitContAsig 
 (
    @UniversalId           udtIdentifier,
    @intVisitId               udtIdentifier,
	@User                  udtStringIdentifier
)

AS
/*
DESCRIPCION: SP para la Entrega de Contenedrores Vacíos
PARAMETROS: 
             @UniversalId    = Id Universal del Contenedor en el Inventario 
             @VisitId        = Id de la visita a la que se asignó la entrega
		     @User           = Usuario que genera la transacción
             
TABLAS :                     
             tblclsContainerInventory
     tblclsContainerDeliveryDetail 
             tblclsServiceOrderItem  
             tblclsService,
                tblContReserv_Inventory
VALORES DE RETORNO:  
FECHA : JUNIO-2009
AUTOR : Luis Islas
*/
DECLARE @FAILURE           INTEGER 
DECLARE @SUCCESS           INTEGER 

SELECT @FAILURE = 1 --c/Errores  
SELECT @SUCCESS = 0 --s/Errores  


SET NOCOUNT ON

IF @@tranchained = 1
        RETURN @FAILURE 

SET TRANSACTION ISOLATION LEVEL 1

DECLARE
         @strContainerId        udtStringIdentifier,
         @intError              udtIdentifier,
         @strIdentifierService  udtStringIdentifier,
         @intVisContItem        udtIdentifier,
         @SOItem                udtIdentifier,
         @ReservId              udtIdentifier,
         @Booking               udtStringIdentifier,
         @Comments              VARCHAR(100),
		@ServiceType           udtIdentifier,
		@ServiceOrderId        udtIdentifier,
		@ServiceOrderItem      udtIdentifier,
		@intServiceQueuId	   udtIdentifier	
    
DECLARE @blnShipConv            INTEGER 
DECLARE @intSOItemId            udtIdentifier
DECLARE @intUniversalId         udtIdentifier
DECLARE @strSOStatusIdentifier  varchar(32)
DECLARE @strContAdmStatIdent    varchar(32)
DECLARE @strTransType           varchar(32)
DECLARE @StatErrSP      		udtIdentifier 
DECLARE @iovalue				udtIdentifier 
declare @StatusId 				udtIdentifier


SELECT 	@intVisContItem = intVisitItemId, 
		@strContainerId = strContainerId, 
		@ServiceOrderId = intServiceOrderId, 
		@ServiceType = intServiceId
FROM  dbo.tblclsVisitContainer
WHERE intVisitId = @intVisitId AND
	   intContainerUniversalId = @UniversalId

--Obtiene el Identifricador del Tipo de Servicio
SELECT @strIdentifierService= strServiceIdentifier
FROM   tblclsService
WHERE  intServiceId = @ServiceType

    --Obtenere el Estatus de Terminado 
SELECT @StatusId  = isnull(intSOStatusId,0)
FROM tblclsServiceOrderStatus
WHERE strSOStatusIdentifier ='TER'

SELECT @strContAdmStatIdent = ''
   --Checa primero que exista la Delivery
    IF NOT EXISTS (SELECT * FROM tblclsContainerDelivery WHERE  intContainerDeliveryId = @ServiceOrderId)
        --Si no existe retorna ERROR
        RETURN(1)
        
    --Obtiene el Id de la Reservación 
    SELECT @ReservId = intContainerReservationId 
      FROM tblclsContainerDelivery 
     WHERE intContainerDeliveryId = @ServiceOrderId

    --Obtiene el Booking            
    SELECT @Booking = strBookingId
      FROM tblclsContainerReservation
     WHERE intContainerReservationId = @ReservId
 

    PRINT "INSERT tblContReserv_Inventory"

    --Insertar si y solo si el Contenedor, Servicio y Numero de Orden no esten ya relacionados con la Reservacion
    IF NOT EXISTS (SELECT * 
                     FROM tblContReserv_Inventory
                    WHERE intContainerReservationId = @ReservId
                      AND intServiceId = @ServiceType
                      AND intContainerUniversalId = @UniversalId
                      AND intServiceOrderId = @ServiceOrderId)
    BEGIN
		BEGIN TRAN	
        -->>Registrar el Contenedor por Entregado  en la Tabla Correspondiente
        INSERT 
          INTO tblContReserv_Inventory
        VALUES (@ReservId, @UniversalId, @ServiceType, @ServiceOrderId, getDate(), getDate(), @User, getDate(), @User)
    END
    ELSE
		RETURN @SUCCESS

    SELECT @intError = @@error 

    IF  @intError <> 0
    BEGIN
        ROLLBACK TRANSACTION
        RETURN @FAILURE
    END

    COMMIT TRAN

	
	BEGIN TRANSACTION 
    --Inserta el el Detalle de la Delivery
    INSERT INTO tblclsContainerDeliveryDetail 
    VALUES (@ServiceOrderId, 
            @UniversalId,
            @strContainerId, 
            @intVisitId, 0, 0, '',
            GETDATE(), @User, GETDATE(), @User)
    
    --Revisa si hubo algún Error
    SELECT @intError = @@error 

    IF  @intError <> 0
    BEGIN
       ROLLBACK TRANSACTION

       RETURN @FAILURE
    END          

    --Validar si ya existe un booking asignado al contenedor
    IF EXISTS(SELECT * FROM tblclsContainerInvBooking WHERE intContainerUniversalId = @UniversalId) BEGIN
        UPDATE  tblclsContainerInvBooking
        SET     strBookingId = @Booking,
                intContInvBookComments = 'Booking asignado por el Empty Depot',
                blnContInvBookMod = 0,
                dtmContInvBookLastModified = GETDATE(),
                strContInvBookLastModifiedBy = @User
        WHERE   intContainerUniversalId = @UniversalId

    END
    ELSE BEGIN
        --Actualiza la tabla en donde relaciona el Bookin con el Univ Id del Cont
        INSERT INTO tblclsContainerInvBooking
        VALUES(@UniversalId,@Booking,0,'Booking asignado por el Empty Depot',GETDATE(),@User,GETDATE(),@User)
    END

    select @intError = @@error 

    IF  @intError <> 0
     BEGIN
       ROLLBACK TRANSACTION
       RETURN @FAILURE
     END

    COMMIT TRAN

    -- Agregar al histórico la transacción
    SELECT @Comments = 'Entrega de vacío con reservación ('+@strIdentifierService+','+CONVERT(VARCHAR(7),@ServiceOrderId)+') , con el Booking: ' + @Booking
    EXECUTE  @intError = spUpdateHistoryOtherData @UniversalId, 27, @Comments, @User

    PRINT "Se proceso el sp spUpdateHistoryOtherData"

    --Si hubo algun error
    IF @intError <> 0
      BEGIN
       --ROLLBACK TRANSACTION
       RETURN @FAILURE
      END



    ---->>Actualizacion de la Maniobra de Entrega de Vacios
    declare @intSOStatusId  NUMERIC
    declare @intContReser    NUMERIC 
    declare @intDelivered   NUMERIC

    --Contenedores Reservados por la Maniobra
    SELECT @intContReser= intContDelAmountReserv     
    FROM tblclsContainerDelivery 
    WHERE intContainerDeliveryId= @ServiceOrderId

    --Contenedors Entregados
    SELECT @intDelivered = count(*) 
    FROM tblclsContainerDeliveryDetail 
    WHERE intContainerDeliveryId= @ServiceOrderId

    --SELECT @intContReser
    --SELECT @intDelivered 

    SELECT @intSOStatusId = IsNull(intSOStatusId, 0)
      FROM tblclsServiceOrderStatus
     WHERE ( ( (@intContReser = @intDelivered) AND (strSOStatusIdentifier = 'TER') ) 
        OR   ( (@intDelivered > 0) AND (@intDelivered < @intContReser) AND (strSOStatusIdentifier ='EJP') ) 
           )

    --Si se Encontro un Estado Mayor a 0 se Actualiza el Estatus
    IF  @intSOStatusId > 0
    BEGIN                              
        BEGIN TRAN
    
        PRINT "UPDATE tblclsContainerDelivery"

        UPDATE tblclsContainerDelivery
           SET intSOStatusId = @intSOStatusId
         WHERE intContainerDeliveryId = @ServiceOrderId                      
        
        SELECT @intError = @@error 

        IF  @intError <> 0 BEGIN
            ROLLBACK TRANSACTION
        
            RETURN @FAILURE
        END
        
        COMMIT TRANSACTION 
    END 

--Se confirma la carga del contenedor y se genera el historico correspondiente   

 --Se entregó el contenedor
    SELECT  @intServiceQueuId = ISNULL(MIN(intServiceQueuId), 0)
    FROM    tblclsServiceQueu
    WHERE   intVisitId = @intVisitId
        AND intServiceId = @ServiceType
        AND intServiceOrderId = @ServiceOrderId
        AND intServiceOrderDetailId = 0
        AND (isnull(tblclsServiceQueu.intContainerUniversalId,0) = 0
            OR (strContainerId IS NULL OR strContainerId = ''))

	IF @intServiceQueuId > 0 
	BEGIN 
	UPDATE  tblclsServiceQueu
	SET     strContainerId = @strContainerId,
			intContainerUniversalId = @UniversalId,
		   intSOStatusId              	= @StatusId  ,
		   blnServiceQueuExecuted     	= 1,
		   dtmServiceQueuExecDate     	= getdate() ,
		   dtmServiceQueuLastModified 	= getDate(),
		   strServiceQueuModifiedBy   	= @User        
	WHERE   intVisitId = @intVisitId
		AND intServiceId = @ServiceType
		AND intServiceOrderId = @ServiceOrderId
		AND intServiceOrderDetailId = 0
		AND intServiceQueuId = @intServiceQueuId
	
	SELECT @intError = @@Error
	END
	
	
	--Si se recupero el Universal Id del Contenedor marcado como completado
	IF @UniversalId>0 
	BEGIN
		SELECT @Comments = 'Carga de Contenedor registrada por ' +  @User
	
		EXECUTE  @intError = spUpdateHistoryOtherData @UniversalId, 27, @Comments,   @User
	END
	
	--Modifica el status fiscal del contenedor
	UPDATE dbo.tblclsContainerInventory 
	SET
		 intContFisStatusId  = (	select intContFisStatusId
								from dbo.tblclsContainerFiscalStatus
								where strContFisStatusIdentifier = "LIBERADO")
	WHERE intContainerUniversalId  = @UniversalId
	
	

    RETURN @SUCCESS

 	



