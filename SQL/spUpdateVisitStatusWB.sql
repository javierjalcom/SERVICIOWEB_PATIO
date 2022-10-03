/*
Highlight and execute the following statement to drop the procedure
before executing the create statement.

DROP PROCEDURE spUpdateVisitStatusWB

*/

CREATE PROCEDURE spUpdateVisitStatusWB   @intVisitId          udtIdentifier
                                        , @strStatus     varchar(10)
                                        , @strUser             udtUserName         
  
AS  
  
   -- si el estatus esta vacio, actualizar el estatus en modo a actualizado , si es que esta en estatus de capturado
      
     IF EXISTS (   SELECT tblclsVisit.intVisitId
                    FROM tblclsVisit
                         INNER JOIN tblclsServiceOrderStatus ON tblclsServiceOrderStatus.intSOStatusId = tblclsVisit.intSOStatusId
                    WHERE tblclsVisit.intVisitId =  @intVisitId
                    AND tblclsServiceOrderStatus.strSOStatusIdentifier = 'CAP'
               )                
                 BEGIN
                 
                     UPDATE  tblclsVisit 
                     SET     tblclsVisit.intSOStatusId = 2
                            ,tblclsVisit.dtmVisitLastModified = GETDATE()
                            ,tblclsVisit.strVisitLastModifiedBy = @strUser                            
                     WHERE  tblclsVisit.intVisitId = @intVisitId
                     
                 END 