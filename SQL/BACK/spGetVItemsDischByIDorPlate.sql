/*
Highlight and execute the following statement to drop the procedure
before executing the create statement.

DROP PROCEDURE spGetVItemsDischByIDorPlate

*/

CREATE PROCEDURE spGetVItemsDischByIDorPlate(  @aintVisitId udtIdentifier, @astrVisitPlate varchar(32)   )
                                   
 AS
   BEGIN
     
    --DESCRIPCION: SP que obtiene los elementos de descarga de una visita , ya sea por numero o por placas 
	
	--TABLAS :  --  tblclsVisit, tblclsVisitContainer , tblclsServiceQueue
                
     -- ARGUMENTOS:
                -- intVisitId .- ID de visita 
                -- strVisitPlate - Numero de placas
                

	--VALORES DE RETORNO:  
	
	/*        
	           intVisitId .- Id de visita
	           strVisitPlate .- placas 
	*/
	
	--FECHA : 06-MAYO-2019
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
               --------------
               ---------SQ---
               SELECT 0 as 'descargado' , SQ.intVisitId,
                        I.strContainerInvYardPositionId, 
                        (CASE WHEN ( SELECT INV.blnContainerIsFull 
                                     FROM tblclsContainerInventory INV 
                                     WHERE INV.intContainerUniversalId  = SQ.intContainerUniversalId 
                                   )  = 1 
                                     THEN 1 
                                     ELSE 0 
                          END) as blnContainerIsFull, 
                          SQ.intContainerUniversalId, 
                          SV.strServiceIdentifier, 
                          SQ.strContainerId, 
                          T.strContainerTypeIdentifier, 
                          S.strContainerSizeIdentifier, 
                          SQ.intServiceOrderId, 
                          SV.strServiceName, 
                          SQ.dtmServiceQueuStartDate, 
                          SQ.dtmServiceQueuExecDate, 
                          SQ.intServiceId, 
                          SQ.intServiceQueuId, 
                          SV.strServiceIdentifier, 
                          (CASE ISNULL(I.intContainerUniversalId,0) 
                                        WHEN 0 THEN 'SIN ESTATUS' 
                                        ELSE CFS.strContFisStatusIdentifier 
                                        END) AS status, 
                           LINE.strShippingLineIdentifier, 
                           I.decContainerInventoryVGM ,
                            SQ.intServiceOrderId, SQ.intServiceQueuId 
                FROM tblclsServiceQueu SQ 
                  INNER JOIN tblclsVisit ON tblclsVisit.intVisitId = SQ.intVisitId
                   LEFT JOIN tblclsContainerInventory I       ON SQ.intContainerUniversalId = I.intContainerUniversalId 
                   LEFT JOIN tblclsContainerFiscalStatus CFS  ON I.intContFisStatusId =CFS.intContFisStatusId 
                  -- LEFT JOIN tblclsContainerCategory CATE     ON I.intContainerCategoryId = CATE.intContainerCategoryId 
                   LEFT JOIN  tblclsShippingLine LINE         ON I.intContainerInvOperatorId = LINE.intShippingLineId 
                   LEFT JOIN tblclsContainer CONT             ON I.strContainerId = CONT.strContainerId 
                   LEFT JOIN tblclsContainerISOCode ISO       ON CONT.intContISOCodeId = ISO.intContISOCodeId 
                   LEFT JOIN tblclsContainerSize S            ON ISO.intContainerSizeId = S.intContainerSizeId 
                   LEFT JOIN tblclsContainerType T            ON ISO.intContainerTypeId = T.intContainerTypeId 
                   LEFT JOIN tblclsService SV                 ON SQ.intServiceId = SV.intServiceId 
                   
                   WHERE  SQ.blnServiceQueuExecuted = 0  AND 
                   SQ.dtmServiceQueuCheckIn IS NOT NULL AND 
                   SQ.dtmServiceQueuCheckOut IS NULL AND    
                   SQ. dtmServiceQueuExecDate IS NULL AND 
                   SQ.intServiceId IN(SELECT SERV.intServiceId 
                                      FROM tblclsService SERV 
                                      WHERE SERV.strServiceIdentifier IN ('RECLL','RECV','RECVOS') )
                   AND  tblclsVisit.strVisitPlate = @astrVisitPlate
                   AND   tblclsVisit.intVisitId  >= @lint_VisitMin
                   AND   tblclsVisit.intVisitId  <= @lint_VisitMax
                   --AND  SQ.intVisitId=@aintVisitId 

               -----------SQ-------
               
	      END 
      ELSE
          	 -- si no buscar por visita 
	      BEGIN
	      
	      
	      ---sq-----
	      
	      ------sq---------------
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