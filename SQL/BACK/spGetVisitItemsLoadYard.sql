/*
Highlight and execute the following statement to drop the procedure
before executing the create statement.

DROP PROCEDURE spGetVisitItemsLoadYard

*/

CREATE PROCEDURE spGetVisitItemsLoadYard(  @aintVisitId udtIdentifier, @astrVisitPlate varchar(32)   )
                                   
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
	DECLARE @lstr_date_in varchar(30)
	DECLARE @lstr_date_out varchar(30)
	DECLARE @lstr_date_exe varchar(30)
	DECLARE @lint_visitvalid udtIdentifier
	

     SET @lint_VisitMax=0
     SET @lint_VisitMin =0 
     SET @lint_visitvalid =-1
     
     
     
     --- buscar por placas 
      IF LEN(@astrVisitPlate)> 3 
	      BEGIN
	      
	           -- obtener el maximo y limite de la visita 
	           SELECT  @lint_VisitMax = MAX( tblclsVisit.intVisitId) 
	           FROM tblclsVisit
	           
               -- obtener el limite inferior
               SET @lint_VisitMin = @lint_VisitMax -1000
               
               
               
               ------------- validar la VISITA------               
               SELECT @lstr_date_in = ISNULL( CONVERT(VARCHAR(30), tblclsVisit.dtmVisitDatetimeIn, 121),'19000101 00:00') ,
                      @lstr_date_out =ISNULL( CONVERT(VARCHAR(30), tblclsVisit.dtmVisitDatetimeOut, 121),'19000101 00:00')
               FROM tblclsVisit               
               WHERE tblclsVisit.strVisitPlate = @astrVisitPlate
               AND   tblclsVisit.intVisitId  >= @lint_VisitMin
               AND   tblclsVisit.intVisitId  <= @lint_VisitMax               
               ---------------
               
                   
                IF (@@ROWCOUNT <>0)
                  BEGIN
                    -- VALIDAR LA VISITA 
                     -- CHECK IN 
                     IF ( @lstr_date_in = '19000101 00:00')
                        BEGIN
                             SELECT 'NO TIENE CHECK IN'
                             SET @lint_visitvalid = 0
                             RETURN 1
                        END 
                        
                     --CHECK OUT
                     IF ( @lstr_date_out <> '19000101 00:00' AND  @lint_visitvalid = -1 )
                        BEGIN
                             SELECT 'YA TIENE CHECK OUT' 
                             SET  @lint_visitvalid=0
                             RETURN 1
                        END --- CHECK OUT
                     
                  END -- TRAJO UN REGISTRO
               --------------------------
               --------------
               ---------SQ---
               SELECT 0 as 'cargado' , 
              		  (CASE WHEN SQ.blnServiceQueuExecuted =1 THEN '1'
               		   ELSE '0'               		  
               		   END
              		  ) as 'blnServiceQueuExecuted'
                 	  , SQ.intVisitId,                       
                       ISNULL(tblclsVisit.dtmVisitDatetimeIn,'19000101 00:00')    as 'dtmVisitDatetimeIn' ,
                       ISNULL(tblclsVisit.dtmVisitDatetimeOut,'19000101 00:00')    as 'dtmVisitDatetimeOut' ,
                       tblclsVisit.strVisitPlate as 'strVisitPlate',
                       tblclsCarrierLine.strCarrierLineIdentifier + ':'+ tblclsCarrierLine.strCarrierLineName AS 'carrierdata' , 
                       tblclsVisit.strVisitDriver as 'strVisitDriver', 
                       tblclsServiceOrderStatus.strSOStatusIdentifier AS 'vsostatus',
                       tblclsVisit.intVisitDriverId as 'intVisitDriverId',
                       I.strContainerInvYardPositionId as 'strContainerInvYardPositionId', 
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
                        SQ.intServiceOrderId, SQ.intServiceQueuId,
                        CATE.strContainerCatIdentifier
                        
                FROM tblclsServiceQueu SQ 
                   INNER JOIN tblclsVisit ON tblclsVisit.intVisitId = SQ.intVisitId
                   INNER JOIN tblclsServiceOrderStatus ON tblclsServiceOrderStatus.intSOStatusId = tblclsVisit.intSOStatusId
                   LEFT JOIN tblclsContainerInventory I       ON SQ.intContainerUniversalId = I.intContainerUniversalId 
                   LEFT JOIN tblclsContainerFiscalStatus CFS  ON I.intContFisStatusId =CFS.intContFisStatusId 
                   LEFT JOIN tblclsContainerCategory CATE     ON I.intContainerCategoryId = CATE.intContainerCategoryId 
                   LEFT JOIN  tblclsShippingLine LINE         ON I.intContainerInvOperatorId = LINE.intShippingLineId 
                   LEFT JOIN tblclsContainer CONT             ON I.strContainerId = CONT.strContainerId 
                   LEFT JOIN tblclsContainerISOCode ISO       ON CONT.intContISOCodeId = ISO.intContISOCodeId 
                   LEFT JOIN tblclsContainerSize S            ON ISO.intContainerSizeId = S.intContainerSizeId 
                   LEFT JOIN tblclsContainerType T            ON ISO.intContainerTypeId = T.intContainerTypeId 
                   LEFT JOIN tblclsService SV                 ON SQ.intServiceId = SV.intServiceId 
                   LEFT JOIN tblclsCarrierLine                ON tblclsCarrierLine.intCarrierLineId = tblclsVisit.intCarrierLineId
                   
                   
            --  WHERE  SQ.blnServiceQueuExecuted = 0  AND 
                WHERE  SQ.dtmServiceQueuCheckIn IS NOT NULL AND 
                   SQ.dtmServiceQueuCheckOut IS NULL AND    
                 --  SQ. dtmServiceQueuExecDate IS NULL AND 
                   SQ.intServiceId IN(SELECT SERV.intServiceId 
                                      FROM tblclsService SERV 
                                      WHERE SERV.strServiceIdentifier IN ('ENTLL','ENTV') )
                   AND  tblclsVisit.strVisitPlate = @astrVisitPlate
                   AND   tblclsVisit.intVisitId  >= @lint_VisitMin
                   AND   tblclsVisit.intVisitId  <= @lint_VisitMax
                   order by SQ.blnServiceQueuExecuted asc
                   
                   --AND  SQ.intVisitId=@aintVisitId 

               -----------SQ-------
               
	      END 
      ELSE
          	 -- si no buscar por visita 
	      BEGIN
	      
	      ---sq-----
	      
	                     ------------- validar la VISITA------               
             SELECT @lstr_date_in = ISNULL( CONVERT(VARCHAR(30), tblclsVisit.dtmVisitDatetimeIn, 121),'19000101 00:00') ,
                      @lstr_date_out =ISNULL( CONVERT(VARCHAR(30), tblclsVisit.dtmVisitDatetimeOut, 121),'19000101 00:00')
               FROM tblclsVisit                           
               WHERE tblclsVisit.intVisitId = @aintVisitId

               ---------------
               
                   
                IF (@@ROWCOUNT <>0)
                  BEGIN
                    -- VALIDAR LA VISITA 
                     -- CHECK IN 
                     IF ( @lstr_date_in = '19000101 00:00')
                        BEGIN
                             SELECT 'NO TIENE CHECK IN'
                             SET  @lint_visitvalid=0
                             RETURN 1
                        END 
                        
                     --CHECK OUT
                     IF ( @lstr_date_out <> '19000101 00:00' AND  @lint_visitvalid = -1 )
                        BEGIN
                             SELECT 'YA TIENE CHECK OUT'
                             SET  @lint_visitvalid=0
                             RETURN 1
                        END --- CHECK OUT
                     
                  END -- TRAJO UN REGISTRO
	      ------sq---------------
	           -- buscar las visitas de las placas en un rango de visitas 
              
                SELECT 0 as 'cargado' ,  
                       (CASE WHEN SQ.blnServiceQueuExecuted =1 THEN '1'
               		     ELSE '0'        
               		    END       		  
              		   ) as 'blnServiceQueuExecuted',
                        SQ.intVisitId,                       
                       ISNULL(tblclsVisit.dtmVisitDatetimeIn,'19000101 00:00')    as 'dtmVisitDatetimeIn' ,
                       ISNULL(tblclsVisit.dtmVisitDatetimeOut,'19000101 00:00')    as 'dtmVisitDatetimeOut' ,
                       tblclsVisit.strVisitPlate as 'strVisitPlate',
                       tblclsCarrierLine.strCarrierLineIdentifier + ':'+ tblclsCarrierLine.strCarrierLineName AS 'carrierdata' , 
                       tblclsVisit.strVisitDriver as 'strVisitDriver', 
                       tblclsServiceOrderStatus.strSOStatusIdentifier AS 'vsostatus',
                       tblclsVisit.intVisitDriverId as 'intVisitDriverId',
                       I.strContainerInvYardPositionId  as 'strContainerInvYardPositionId' , 
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
                        SQ.intServiceOrderId, SQ.intServiceQueuId ,
                         CATE.strContainerCatIdentifier
                         
                FROM tblclsServiceQueu SQ 
                   INNER JOIN tblclsVisit ON tblclsVisit.intVisitId = SQ.intVisitId
                   INNER JOIN tblclsServiceOrderStatus ON tblclsServiceOrderStatus.intSOStatusId = tblclsVisit.intSOStatusId
                   LEFT JOIN tblclsContainerInventory I       ON SQ.intContainerUniversalId = I.intContainerUniversalId 
                   LEFT JOIN tblclsContainerFiscalStatus CFS  ON I.intContFisStatusId =CFS.intContFisStatusId 
                   LEFT JOIN tblclsContainerCategory CATE     ON I.intContainerCategoryId = CATE.intContainerCategoryId 
                   LEFT JOIN  tblclsShippingLine LINE         ON I.intContainerInvOperatorId = LINE.intShippingLineId 
                   LEFT JOIN tblclsContainer CONT             ON I.strContainerId = CONT.strContainerId 
                   LEFT JOIN tblclsContainerISOCode ISO       ON CONT.intContISOCodeId = ISO.intContISOCodeId 
                   LEFT JOIN tblclsContainerSize S            ON ISO.intContainerSizeId = S.intContainerSizeId 
                   LEFT JOIN tblclsContainerType T            ON ISO.intContainerTypeId = T.intContainerTypeId 
                   LEFT JOIN tblclsService SV                 ON SQ.intServiceId = SV.intServiceId 
                   LEFT JOIN tblclsCarrierLine                ON tblclsCarrierLine.intCarrierLineId = tblclsVisit.intCarrierLineId
                  
                   
             --   WHERE  SQ.blnServiceQueuExecuted = 0  AND 
               WHERE  SQ.dtmServiceQueuCheckIn IS NOT NULL AND 
                      SQ.dtmServiceQueuCheckOut IS NULL AND    
                    --  SQ.dtmServiceQueuExecDate IS NULL AND 
                      SQ.intServiceId IN(SELECT SERV.intServiceId 
                                      FROM tblclsService SERV 
                                      WHERE SERV.strServiceIdentifier IN ('ENTLL','ENTV') )
                   AND  tblclsVisit.intVisitId = @aintVisitId
                   
                   order by SQ.blnServiceQueuExecuted asc
	        
	        
	      END  --ELSE END 

     -----------------------------
      
   END

--GO