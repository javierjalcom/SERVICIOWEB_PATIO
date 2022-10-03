/*
Highlight and execute the following statement to drop the procedure
before executing the create statement.

DROP PROCEDURE spGetVisitItemsByIDorPlate

*/

CREATE PROCEDURE spGetVisitItemsByIDorPlate(  @aintVisitId udtIdentifier, @astrVisitPlate varchar(32)   )
                                   
 AS
   BEGIN
     
    --DESCRIPCION: SP que obtiene los elementos de una visita , ya sea por numero o por placas 
	
	--TABLAS :  --  tblclsVisit, tblclsVisitContainer
                
     -- ARGUMENTOS:
                -- intVisitId .- ID de visita 
                -- strVisitPlate - Numero de placas

	--VALORES DE RETORNO:  
	
	/*        
	           intVisitId .- Id de visita
	           strVisitPlate .- placas 
	*/
	
	--FECHA : 13-MARZO-2019
	--AUTOR : javier.cadena
	
	DECLARE @lint_VisitMax   udtIdentifier
	DECLARE @lint_VisitMin  udtIdentifier
	
     SET @lint_VisitMax=0
     SET @lint_VisitMin =0 
     
     
     --- buscar por placas 
      IF LEN(@astrVisitPlate)> 3 
	      BEGIN
	      
	           -- obtener el maximo y limite de la visita 
	           SELECT  @lint_VisitMax = MAX( tblclsVisit.intVisitId) 
	           FROM tblclsVisit
	           
               -- obtener el limite inferior
               SET @lint_VisitMin = @lint_VisitMax -1000
               
               -- buscar las visitas de las placas en un rango de visitas 
               SELECT  ISNULL(tblclsVisit.intVisitId,0)  as 'intVisitId', 
                       ISNULL(tblclsVisit.dtmVisitDatetimeIn,'19000101 00:00')    as 'dtmVisitDatetimeIn' ,
                       ISNULL(tblclsVisit.dtmVisitDatetimeOut,'19000101 00:00')    as 'dtmVisitDatetimeOut' ,
                       ISNULL(tblclsContainerInventory.blnContainerInvActive,0) AS 'intActive',
                       tblclsVisit.strVisitPlate as 'strVisitPlate',
                       tblclsCarrierLine.strCarrierLineIdentifier + ':'+ tblclsCarrierLine.strCarrierLineName AS 'carrierdata' , 
                       tblclsVisit.strVisitDriver as 'strVisitDriver',                       
                       tblclsServiceOrderStatus.strSOStatusIdentifier AS 'vsostatus',
                       tblclsVisit.intVisitDriverId as 'intVisitDriverId',
                       tblclsVisit.strVisitDriverLicenceNumber as 'strVisitDriverLicenceNumber',
                       tblclsVisitContainer.strContainerId AS 'strContainerId', 
                       ISNULL(tblclsVisitContainer.intContainerUniversalId,0) AS 'intContainerUniversalId',
                       ISNULL(tblclsService.strServiceIdentifier,'') AS 'strServiceIdentifier', 
                       ISNULL(tblclsContainerType.strContainerTypeIdentifier,'') AS 'strContainerTypeIdentifier',
                       ISNULL(tblclsContainerSize.strContainerSizeIdentifier,'') AS 'strContainerSizeIdentifier',
                       ISNULL(tblclsContainerFiscalStatus.strContFisStatusIdentifier,'') AS 'strContFisStatusIdentifier',
                       ISNULL(tblclsContainerAdmStatus.strContAdmStatusIdentifier,'') AS 'strContAdmStatusIdentifier',                       
                       ISNULL(tblclsEIR.intEIRId,0) AS 'intEIRId',
                       tblclsVisitContainer.intServiceOrderId as 'intServiceOrderId'
                       
               FROM tblclsVisit
                INNER JOIN tblclsServiceOrderStatus ON tblclsServiceOrderStatus.intSOStatusId = tblclsVisit.intSOStatusId 
                INNER JOIN tblclsCarrierLine ON tblclsCarrierLine.intCarrierLineId = tblclsVisit.intCarrierLineId
                
                LEFT JOIN tblclsVisitContainer on tblclsVisit.intVisitId = tblclsVisitContainer.intVisitId
                LEFT JOIN tblclsService  ON  tblclsVisitContainer.intServiceId =  tblclsService.intServiceId
                LEFT JOIN tblclsContainer ON tblclsContainer.strContainerId = tblclsVisitContainer.strContainerId
                LEFT JOIN tblclsContainerISOCode ON tblclsContainerISOCode.intContISOCodeId = tblclsContainer.intContISOCodeId
                LEFT JOIN tblclsContainerType ON tblclsContainerType.intContainerTypeId = tblclsContainerISOCode.intContainerTypeId
                LEFT JOIN tblclsContainerSize ON tblclsContainerSize.intContainerSizeId = tblclsContainerISOCode.intContainerSizeId    
                LEFT JOIN tblclsEIR ON   tblclsEIR.intVisitId = tblclsVisitContainer.intVisitId
                                    AND  tblclsEIR.strContainerId = tblclsVisitContainer.strContainerId
                
                LEFT JOIN tblclsContainerInventory ON tblclsContainerInventory.intContainerUniversalId = tblclsVisitContainer.intContainerUniversalId
                LEFT JOIN tblclsContainerFiscalStatus ON tblclsContainerFiscalStatus.intContFisStatusId = tblclsContainerInventory.intContFisStatusId
                LEFT JOIN tblclsContainerAdmStatus    ON tblclsContainerAdmStatus.intContAdmStatusId =  tblclsContainerInventory.intContAdmStatusId


               WHERE tblclsVisit.strVisitPlate = @astrVisitPlate
               AND   tblclsVisit.intVisitId  >= @lint_VisitMin
               AND   tblclsVisit.intVisitId  <= @lint_VisitMax
               
	      END 
      ELSE
          	 -- si no buscar por visita 
	      BEGIN
	           -- buscar las visitas de las placas en un rango de visitas 
               SELECT  ISNULL(tblclsVisit.intVisitId,0)  as 'intVisitId', 
                       ISNULL(tblclsVisit.dtmVisitDatetimeIn,'19000101 00:00')    as 'dtmVisitDatetimeIn' ,
                       ISNULL(tblclsVisit.dtmVisitDatetimeOut,'19000101 00:00')    as 'dtmVisitDatetimeOut' ,
                       ISNULL(tblclsContainerInventory.blnContainerInvActive,0) AS 'intActive',
                       tblclsVisit.strVisitPlate as 'strVisitPlate',
                       tblclsCarrierLine.strCarrierLineIdentifier + ':'+ tblclsCarrierLine.strCarrierLineName AS 'carrierdata' , 
                       tblclsVisit.strVisitDriver as 'strVisitDriver', 
                       tblclsServiceOrderStatus.strSOStatusIdentifier AS 'vsostatus',
                       tblclsVisit.intVisitDriverId as 'intVisitDriverId',
                       tblclsVisit.strVisitDriverLicenceNumber as 'strVisitDriverLicenceNumber',
                       tblclsVisitContainer.strContainerId AS 'strContainerId', 
                       ISNULL(tblclsVisitContainer.intContainerUniversalId,0) AS 'intContainerUniversalId',
                       ISNULL(tblclsService.strServiceIdentifier,'') AS 'strServiceIdentifier', 
                       ISNULL(tblclsContainerType.strContainerTypeIdentifier,'') AS 'strContainerTypeIdentifier',
                       ISNULL(tblclsContainerSize.strContainerSizeIdentifier,'') AS 'strContainerSizeIdentifier',
                       ISNULL(tblclsContainerFiscalStatus.strContFisStatusIdentifier,'') AS 'strContFisStatusIdentifier',
                       ISNULL(tblclsContainerAdmStatus.strContAdmStatusIdentifier,'') AS 'strContAdmStatusIdentifier',                       
                       ISNULL(tblclsEIR.intEIRId,0) AS 'intEIRId',
                       tblclsVisitContainer.intServiceOrderId as 'intServiceOrderId'
                       
               FROM tblclsVisit
                INNER JOIN tblclsServiceOrderStatus ON tblclsServiceOrderStatus.intSOStatusId = tblclsVisit.intSOStatusId 
                INNER JOIN tblclsCarrierLine ON tblclsCarrierLine.intCarrierLineId = tblclsVisit.intCarrierLineId
                
                LEFT JOIN tblclsVisitContainer on tblclsVisit.intVisitId = tblclsVisitContainer.intVisitId
                LEFT JOIN tblclsService  ON  tblclsVisitContainer.intServiceId =  tblclsService.intServiceId
                LEFT JOIN tblclsContainer ON tblclsContainer.strContainerId = tblclsVisitContainer.strContainerId
                LEFT JOIN tblclsContainerISOCode ON tblclsContainerISOCode.intContISOCodeId = tblclsContainer.intContISOCodeId
                LEFT JOIN tblclsContainerType ON tblclsContainerType.intContainerTypeId = tblclsContainerISOCode.intContainerTypeId
                LEFT JOIN tblclsContainerSize ON tblclsContainerSize.intContainerSizeId = tblclsContainerISOCode.intContainerSizeId    
                LEFT JOIN tblclsEIR ON   tblclsEIR.intVisitId = tblclsVisitContainer.intVisitId
                                    AND  tblclsEIR.strContainerId = tblclsVisitContainer.strContainerId
                
                LEFT JOIN tblclsContainerInventory ON tblclsContainerInventory.intContainerUniversalId = tblclsVisitContainer.intContainerUniversalId
                LEFT JOIN tblclsContainerFiscalStatus ON tblclsContainerFiscalStatus.intContFisStatusId = tblclsContainerInventory.intContFisStatusId
                LEFT JOIN tblclsContainerAdmStatus    ON tblclsContainerAdmStatus.intContAdmStatusId =  tblclsContainerInventory.intContAdmStatusId


               WHERE tblclsVisit.intVisitId = @aintVisitId
	        
	        
	      END  --ELSE END 

     -----------------------------
      
   END

--GO

