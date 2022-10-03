/*
Highlight and execute the following statement to drop the procedure
before executing the create statement.

DROP PROCEDURE spGetVisitGCByIDorPlate

*/

CREATE PROCEDURE spGetVisitGCByIDorPlate(  @aintVisitId udtIdentifier, @astrVisitPlate varchar(32)   )
                                   
 AS
   BEGIN
     
    --DESCRIPCION: SP que obtiene los elementos de carga general 
	
	--TABLAS :  --  tblclsVisit, tblclsvisitGenealCargo
                
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
	DECLARE @lint_VisitMin   udtIdentifier
	DECLARE @lstr_service    varchar(20)
	
	
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
               
               --primero ver cual es el servicio de la visita 
	               SELECT @lstr_service =  MAX(tblclsService.strServiceIdentifier)
	               FROM tblclsVisitGeneralCargo
	                 INNER JOIN tblclsVisit   ON tblclsVisit.intVisitId = tblclsVisitGeneralCargo.intVisitId
	                 INNER JOIN tblclsService ON tblclsService.intServiceId = tblclsVisitGeneralCargo.intServiceId               
                   WHERE tblclsVisit.strVisitPlate = @astrVisitPlate
		               AND   tblclsVisit.intVisitId  >= @lint_VisitMin
		               AND   tblclsVisit.intVisitId  <= @lint_VisitMax
		               
             	--- SI ES ENTREGA DE CARGA GENERAL 		               
		        ---  SI ES RECEPCION DE CARGA GENEAL
		        IF @lstr_service = 'RECCG'
		         BEGIN
		         
	               SELECT  ISNULL(tblclsVisit.intVisitId,0)  as 'intVisitId', 
	                       ISNULL(tblclsVisit.dtmVisitDatetimeIn,'19000101 00:00')    as 'dtmVisitDatetimeIn' ,
	                       ISNULL(tblclsVisit.dtmVisitDatetimeOut,'19000101 00:00')    as 'dtmVisitDatetimeOut' ,
	                       ISNULL(tblclsGCInventoryItem.blnGCInvItemActive,0) AS 'intActive',
	                       tblclsVisit.strVisitPlate as 'strVisitPlate',
	                       tblclsCarrierLine.strCarrierLineIdentifier + ':'+ tblclsCarrierLine.strCarrierLineName AS 'carrierdata' , 
	                       tblclsVisit.strVisitDriver as 'strVisitDriver',                       
	                       tblclsServiceOrderStatus.strSOStatusIdentifier AS 'vsostatus',
	                       tblclsVisit.intVisitDriverId as 'intVisitDriverId',
	                       tblclsVisit.strVisitDriverLicenceNumber as 'strVisitDriverLicenceNumber',
	                       ISNULL(tblclsService.strServiceIdentifier,'') AS 'strServiceIdentifier',
	                       ISNULL(tblclsProduct.strProductName,'') AS 'strProductName',
	                       tblclsVisitGeneralCargo.intVisitGCQuantity AS 'intVisitGCQuantity',
	                       tblclsVisitGeneralCargo.decVisitGCWeight AS 'decVisitGCWeight',	                       
	                       tblclsVisitGeneralCargo.intServiceOrderId as 'intServiceOrderId'
	                       
	               FROM tblclsVisit
	                INNER JOIN tblclsServiceOrderStatus ON tblclsServiceOrderStatus.intSOStatusId = tblclsVisit.intSOStatusId 
	                INNER JOIN tblclsCarrierLine ON tblclsCarrierLine.intCarrierLineId = tblclsVisit.intCarrierLineId
	                
	                INNER JOIN tblclsVisitGeneralCargo ON tblclsVisitGeneralCargo.intVisitId = tblclsVisit.intVisitId
	                LEFT JOIN tblclsService  ON  tblclsVisitGeneralCargo.intServiceId =  tblclsService.intServiceId
	                LEFT JOIN tblclsGeneralCargoReception ON tblclsGeneralCargoReception.intGeneralCargoReceptionId =  tblclsVisitGeneralCargo.intServiceOrderId
	                                                      AND tblclsGeneralCargoReception.intServiceId = tblclsVisitGeneralCargo.intServiceId
	                
	                LEFT JOIN tblclsGCReceptionDetail ON   tblclsGCReceptionDetail.intGeneralCargoReceptionId = tblclsVisitGeneralCargo.intServiceOrderId
	                                                  AND  tblclsGCReceptionDetail.intGCReceptionDetailId     = tblclsVisitGeneralCargo.intServiceOrderId
	                                                  AND  tblclsGCReceptionDetail.intGeneralCargoReceptionId =  tblclsGeneralCargoReception.intGeneralCargoReceptionId 
	                                                  
	                LEFT JOIN tblclsProduct  ON  tblclsProduct.intProductId = tblclsGCReceptionDetail.intProductId
	                LEFT JOIN tblclsGeneralCargoInventory ON tblclsVisitGeneralCargo.intGeneralCargoUniversalId = tblclsGeneralCargoInventory.intGeneralCargoUniversalId
	                LEFT JOIN tblclsGCInventoryItem ON   tblclsGCInventoryItem.intGCInventoryItemId       = tblclsVisitGeneralCargo.intGCInventoryItemId
                                                    AND	 tblclsGCInventoryItem.intGeneralCargoUniversalId = tblclsVisitGeneralCargo.intGeneralCargoUniversalId

                    LEFT JOIN tblclsGCAdministrativeStatus ON tblclsGCAdministrativeStatus.intGCAdmStatusId = tblclsGCInventoryItem.intGCAdmStatusId
                    LEFT JOIN tblclsGeneralCargoFiscalStat ON tblclsGeneralCargoFiscalStat.intGCFiscalStatusId  = tblclsGCInventoryItem.intGCFiscalStatusId
                    
	
	               WHERE tblclsVisit.strVisitPlate = @astrVisitPlate
	               AND   tblclsVisit.intVisitId  >= @lint_VisitMin
	               AND   tblclsVisit.intVisitId  <= @lint_VisitMax
                    		         
		         END  -- IF @lstr_service = 'RECCG'
		         
		        IF @lstr_service = 'ENTCG'		        
		         BEGIN

			         
	               SELECT  ISNULL(tblclsVisit.intVisitId,0)  as 'intVisitId', 
	                       ISNULL(tblclsVisit.dtmVisitDatetimeIn,'19000101 00:00')    as 'dtmVisitDatetimeIn' ,
	                       ISNULL(tblclsVisit.dtmVisitDatetimeOut,'19000101 00:00')    as 'dtmVisitDatetimeOut' ,
	                       ISNULL(tblclsGCInventoryItem.blnGCInvItemActive,0) AS 'intActive',
	                       tblclsVisit.strVisitPlate as 'strVisitPlate',
	                       tblclsCarrierLine.strCarrierLineIdentifier + ':'+ tblclsCarrierLine.strCarrierLineName AS 'carrierdata' , 
	                       tblclsVisit.strVisitDriver as 'strVisitDriver',                       
	                       tblclsServiceOrderStatus.strSOStatusIdentifier AS 'vsostatus',
	                       tblclsVisit.intVisitDriverId as 'intVisitDriverId',
	                       tblclsVisit.strVisitDriverLicenceNumber as 'strVisitDriverLicenceNumber',
	                       ISNULL(tblclsService.strServiceIdentifier,'') AS 'strServiceIdentifier',
	                       ISNULL(tblclsProduct.strProductName,'') AS 'strProductName',
	                       tblclsVisitGeneralCargo.intVisitGCQuantity AS 'intVisitGCQuantity',
	                       tblclsVisitGeneralCargo.decVisitGCWeight AS 'decVisitGCWeight',	                       
	                       tblclsVisitGeneralCargo.intServiceOrderId  as 'intServiceOrderId'
	                       
	               FROM tblclsVisit
	                INNER JOIN tblclsServiceOrderStatus ON tblclsServiceOrderStatus.intSOStatusId = tblclsVisit.intSOStatusId 
	                INNER JOIN tblclsCarrierLine ON tblclsCarrierLine.intCarrierLineId = tblclsVisit.intCarrierLineId
	                
	                LEFT JOIN tblclsVisitGeneralCargo ON tblclsVisitGeneralCargo.intVisitId = tblclsVisit.intVisitId
	                LEFT JOIN tblclsService  ON  tblclsVisitGeneralCargo.intServiceId =  tblclsService.intServiceId
	                
	                LEFT JOIN tblclsGeneralCargoDelivery ON tblclsGeneralCargoDelivery.intGeneralCargoDeliveryId  =  tblclsVisitGeneralCargo.intServiceOrderId
	                                                     AND tblclsGeneralCargoDelivery.intServiceId = tblclsVisitGeneralCargo.intServiceId
	                
	                LEFT JOIN tblclsGCDeliveryDetail  ON   tblclsGCDeliveryDetail.intGCDeliveryDetailId     = tblclsVisitGeneralCargo.intServiceOrderDetailId
	                                                  AND  tblclsGCDeliveryDetail.intGeneralCargoDeliveryId = tblclsGeneralCargoDelivery.intGeneralCargoDeliveryId
	                                                  AND  tblclsGCDeliveryDetail.intGeneralCargoDeliveryId =  tblclsVisitGeneralCargo.intServiceOrderId
	                                                  

	                LEFT JOIN tblclsGeneralCargoInventory ON tblclsVisitGeneralCargo.intGeneralCargoUniversalId = tblclsGeneralCargoInventory.intGeneralCargoUniversalId
	                LEFT JOIN tblclsGCInventoryItem ON   tblclsGCInventoryItem.intGCInventoryItemId       = tblclsVisitGeneralCargo.intGCInventoryItemId
                                                    AND	 tblclsGCInventoryItem.intGeneralCargoUniversalId = tblclsVisitGeneralCargo.intGeneralCargoUniversalId
                   LEFT JOIN tblclsProduct  ON  tblclsProduct.intProductId = tblclsGeneralCargoInventory.intProductId

                    LEFT JOIN tblclsGCAdministrativeStatus ON tblclsGCAdministrativeStatus.intGCAdmStatusId = tblclsGCInventoryItem.intGCAdmStatusId
                    LEFT JOIN tblclsGeneralCargoFiscalStat ON tblclsGeneralCargoFiscalStat.intGCFiscalStatusId  = tblclsGCInventoryItem.intGCFiscalStatusId
                    
	
	               WHERE tblclsVisit.strVisitPlate = @astrVisitPlate
	               AND   tblclsVisit.intVisitId  >= @lint_VisitMin
	               AND   tblclsVisit.intVisitId  <= @lint_VisitMax
	                    		         		               
		                
		         END  -- IF @lstr_service  'ENTCG
		        
               
	      END 
      ELSE
          	 -- si no buscar por visita 
	      BEGIN
	      
	           --- obtener el servicio maximo 
	             --primero ver cual es el servicio de la visita 
	               
	               SELECT @lstr_service =  MAX(tblclsService.strServiceIdentifier)
	               FROM tblclsVisitGeneralCargo
	                 INNER JOIN tblclsVisit   ON tblclsVisit.intVisitId = tblclsVisitGeneralCargo.intVisitId
	                 INNER JOIN tblclsService ON tblclsService.intServiceId = tblclsVisitGeneralCargo.intServiceId               
                   WHERE tblclsVisit.intVisitId = @aintVisitId
		       ---------------------------------------------------------
		       
		       -- hacer un select por el servicio 
		       ------- ingreso de carga
		       IF @lstr_service = 'RECCG'
		         BEGIN
		          SELECT  ISNULL(tblclsVisit.intVisitId,0)  as 'intVisitId', 
	                       ISNULL(tblclsVisit.dtmVisitDatetimeIn,'19000101 00:00')    as 'dtmVisitDatetimeIn' ,
	                       ISNULL(tblclsVisit.dtmVisitDatetimeOut,'19000101 00:00')    as 'dtmVisitDatetimeOut' ,
	                       ISNULL(tblclsGCInventoryItem.blnGCInvItemActive,0) AS 'intActive',
	                       tblclsVisit.strVisitPlate as 'strVisitPlate',
	                       tblclsCarrierLine.strCarrierLineIdentifier + ':'+ tblclsCarrierLine.strCarrierLineName AS 'carrierdata' , 
	                       tblclsVisit.strVisitDriver as 'strVisitDriver',                       
	                       tblclsServiceOrderStatus.strSOStatusIdentifier AS 'vsostatus',
	                       tblclsVisit.intVisitDriverId as 'intVisitDriverId',
	                       tblclsVisit.strVisitDriverLicenceNumber as 'strVisitDriverLicenceNumber',
	                       ISNULL(tblclsService.strServiceIdentifier,'') AS 'strServiceIdentifier',
	                       ISNULL(tblclsProduct.strProductName,'') AS 'strProductName',
	                       tblclsVisitGeneralCargo.intVisitGCQuantity AS 'intVisitGCQuantity',
	                       tblclsVisitGeneralCargo.decVisitGCWeight AS 'decVisitGCWeight',	                       
	                       tblclsVisitGeneralCargo.intServiceOrderId as 'intServiceOrderId'
	                       
	               FROM tblclsVisit
	                INNER JOIN tblclsServiceOrderStatus ON tblclsServiceOrderStatus.intSOStatusId = tblclsVisit.intSOStatusId 
	                INNER JOIN tblclsCarrierLine ON tblclsCarrierLine.intCarrierLineId = tblclsVisit.intCarrierLineId
	                
	                INNER JOIN tblclsVisitGeneralCargo ON tblclsVisitGeneralCargo.intVisitId = tblclsVisit.intVisitId
	                LEFT JOIN tblclsService  ON  tblclsVisitGeneralCargo.intServiceId =  tblclsService.intServiceId
	                LEFT JOIN tblclsGeneralCargoReception ON tblclsGeneralCargoReception.intGeneralCargoReceptionId =  tblclsVisitGeneralCargo.intServiceOrderId
	                                                      AND tblclsGeneralCargoReception.intServiceId = tblclsVisitGeneralCargo.intServiceId
	                
	                LEFT JOIN tblclsGCReceptionDetail ON   tblclsGCReceptionDetail.intGeneralCargoReceptionId = tblclsVisitGeneralCargo.intServiceOrderId
	                                                  AND  tblclsGCReceptionDetail.intGCReceptionDetailId     = tblclsVisitGeneralCargo.intServiceOrderId
	                                                  AND  tblclsGCReceptionDetail.intGeneralCargoReceptionId =  tblclsGeneralCargoReception.intGeneralCargoReceptionId 
	                                                  
	                LEFT JOIN tblclsProduct  ON  tblclsProduct.intProductId = tblclsGCReceptionDetail.intProductId
	                LEFT JOIN tblclsGeneralCargoInventory ON tblclsVisitGeneralCargo.intGeneralCargoUniversalId = tblclsGeneralCargoInventory.intGeneralCargoUniversalId
	                LEFT JOIN tblclsGCInventoryItem ON   tblclsGCInventoryItem.intGCInventoryItemId       = tblclsVisitGeneralCargo.intGCInventoryItemId
                                                    AND	 tblclsGCInventoryItem.intGeneralCargoUniversalId = tblclsVisitGeneralCargo.intGeneralCargoUniversalId

                    LEFT JOIN tblclsGCAdministrativeStatus ON tblclsGCAdministrativeStatus.intGCAdmStatusId = tblclsGCInventoryItem.intGCAdmStatusId
                    LEFT JOIN tblclsGeneralCargoFiscalStat ON tblclsGeneralCargoFiscalStat.intGCFiscalStatusId  = tblclsGCInventoryItem.intGCFiscalStatusId
                    
	
	               WHERE  tblclsVisit.intVisitId = @aintVisitId
	             
		         
		         END  --  IF @lstr_service = 'RECCG'
		       ---------------------------------------
		         
		       ---  salida de carga 
     		   IF @lstr_service = 'ENTCG'
		         BEGIN
		         
		         
	               SELECT  ISNULL(tblclsVisit.intVisitId,0)  as 'intVisitId', 
	                       ISNULL(tblclsVisit.dtmVisitDatetimeIn,'19000101 00:00')    as 'dtmVisitDatetimeIn' ,
	                       ISNULL(tblclsVisit.dtmVisitDatetimeOut,'19000101 00:00')    as 'dtmVisitDatetimeOut' ,
	                       ISNULL(tblclsGCInventoryItem.blnGCInvItemActive,0) AS 'intActive',
	                       tblclsVisit.strVisitPlate as 'strVisitPlate',
	                       tblclsCarrierLine.strCarrierLineIdentifier + ':'+ tblclsCarrierLine.strCarrierLineName AS 'carrierdata' , 
	                       tblclsVisit.strVisitDriver as 'strVisitDriver',                       
	                       tblclsServiceOrderStatus.strSOStatusIdentifier AS 'vsostatus',
	                       tblclsVisit.intVisitDriverId as 'intVisitDriverId',
	                       tblclsVisit.strVisitDriverLicenceNumber as 'strVisitDriverLicenceNumber',
	                       ISNULL(tblclsService.strServiceIdentifier,'') AS 'strServiceIdentifier',
	                       ISNULL(tblclsProduct.strProductName,'') AS 'strProductName',
	                       tblclsVisitGeneralCargo.intVisitGCQuantity AS 'intVisitGCQuantity',
	                       tblclsVisitGeneralCargo.decVisitGCWeight AS 'decVisitGCWeight',	                       
	                       tblclsVisitGeneralCargo.intServiceOrderId as 'intServiceOrderId'
	                       
	               FROM tblclsVisit
	                INNER JOIN tblclsServiceOrderStatus ON tblclsServiceOrderStatus.intSOStatusId = tblclsVisit.intSOStatusId 
	                INNER JOIN tblclsCarrierLine ON tblclsCarrierLine.intCarrierLineId = tblclsVisit.intCarrierLineId
	                
	                LEFT JOIN tblclsVisitGeneralCargo ON tblclsVisitGeneralCargo.intVisitId = tblclsVisit.intVisitId
	                LEFT JOIN tblclsService  ON  tblclsVisitGeneralCargo.intServiceId =  tblclsService.intServiceId
	                
	                LEFT JOIN tblclsGeneralCargoDelivery ON tblclsGeneralCargoDelivery.intGeneralCargoDeliveryId  =  tblclsVisitGeneralCargo.intServiceOrderId
	                                                     AND tblclsGeneralCargoDelivery.intServiceId = tblclsVisitGeneralCargo.intServiceId
	                
	                LEFT JOIN tblclsGCDeliveryDetail  ON   tblclsGCDeliveryDetail.intGCDeliveryDetailId     = tblclsVisitGeneralCargo.intServiceOrderDetailId
	                                                  AND tblclsGCDeliveryDetail.intGeneralCargoDeliveryId  = tblclsGeneralCargoDelivery.intGeneralCargoDeliveryId
	                                                  AND  tblclsGCDeliveryDetail.intGeneralCargoDeliveryId = tblclsGeneralCargoDelivery.intGeneralCargoDeliveryId
	                                                  

	                LEFT JOIN tblclsGeneralCargoInventory ON tblclsVisitGeneralCargo.intGeneralCargoUniversalId = tblclsGeneralCargoInventory.intGeneralCargoUniversalId
	                LEFT JOIN tblclsGCInventoryItem ON   tblclsGCInventoryItem.intGCInventoryItemId       = tblclsVisitGeneralCargo.intGCInventoryItemId
                                                    AND	 tblclsGCInventoryItem.intGeneralCargoUniversalId = tblclsVisitGeneralCargo.intGeneralCargoUniversalId
                   LEFT JOIN tblclsProduct  ON  tblclsProduct.intProductId = tblclsGeneralCargoInventory.intProductId

                    LEFT JOIN tblclsGCAdministrativeStatus ON tblclsGCAdministrativeStatus.intGCAdmStatusId = tblclsGCInventoryItem.intGCAdmStatusId
                    LEFT JOIN tblclsGeneralCargoFiscalStat ON tblclsGeneralCargoFiscalStat.intGCFiscalStatusId  = tblclsGCInventoryItem.intGCFiscalStatusId
                    
	
	               WHERE  tblclsVisit.intVisitId = @aintVisitId
		         
		         END  --  IF @lstr_service = 'ENTCG'
		       ----------------------------------------
              
	        
	      END  --ELSE END 

     -----------------------------
      
   END

--GO
