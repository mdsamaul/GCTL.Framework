USE [GCTL_ERP_DB_FORMOSA_2020_260923]
GO
/****** Object:  StoredProcedure [dbo].[DocumentationReport]    Script Date: 9/27/2026 5:06:34 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
 
 ALTER Proc [dbo].[DocumentationReport]
 @DateUse  nvarchar(50),
 @fromDate  nvarchar(50),
 @toDate  nvarchar(50),
 @CustomerID  nvarchar(50),
 @PortId  nvarchar(50),
 @ExpenseTypeID  nvarchar(50),
 @JobNo  nvarchar(50)
 -- DocumentationReport 'N','','','','','0001',''
 as
-- with cta as 
--(select  dju.UpdateStatus,dju.JobNo 
--from CF_DailyJobUpdateEntry dju 
--Where dju.JobNo = dju.JobNo 
--AND dju.UpdateDateTime = 
--(Select Max(mu.UpdateDateTime) 
--From CF_DailyJobUpdateEntry mu Where dju.JobNo = mu.JobNo)) 
 with cta as 
(Select distinct LTRIM(STUFF((Select ', '+ CONCAT(FORMAT(UpdateDateTime,'dd/MM/yyyy hh:mm:ss tt'),': ',UpdateStatus)   
from CF_DailyJobUpdateEntry  Where JobNo=dju.JobNo
For XML PATH('')),1,1,'')) UpdateStatus,dju.JobNo From CF_DailyJobUpdateEntry dju ) 
Select distinct d.JobNo, p.PrifixName
,IIF(FORMAT(d.[Date],'dd/MM/yyyy')='01/01/1900','',FORMAT(d.[Date],'dd/MM/yyyy')) JobDate
, ctm.CustomerName
,  ctm.CustomerAddress
,pn.PortName
,IIF(FORMAT(d.DocReceivedDate,'dd/MM/yyyy')='01/01/1900','',FORMAT(d.DocReceivedDate,'dd/MM/yyyy')) DocReceivedDate
, d.LCNo
,d.BDTRate
,c.ShortName + ' ' + CAST(d.LCValue as nvarchar(50)) as LCValue
, CAST(d.LCValue as decimal(18,2)) as DataColumn1
, c.ShortName as DataColumn2
,IPNo
,IIF(FORMAT(d.IPDate,'dd/MM/yyyy')='01/01/1900','',FORMAT(d.IPDate,'dd/MM/yyyy')) IPDate
, d.UndertakingNo
,d.InvoiceNo
,d.BillNo
,d.BENo
,IIF(FORMAT(d.BLDate,'dd/MM/yyyy')='01/01/1900','',FORMAT(d.BLDate,'dd/MM/yyyy')) BLDate
,IIF(FORMAT(d.BEDate,'dd/MM/yyyy')='01/01/1900','',FORMAT(d.BEDate,'dd/MM/yyyy')) BEDate
,s.SupplierName
, d.PONo
,d.ContainerNo
, d.MaterialDescription
,CONCAT(CAST(d.Quntity1  as nvarchar(50)),' ', u1.UnitTypeName) as Quntity1
,  CONCAT(CAST(d.Quntity2 as nvarchar(50)) , ' ' , u2.UnitTypeName) as Quntity2
, d.FWDR_ShippingAgent
, d.Vessel_RottNo
,IIF(FORMAT(d.ETA,'dd/MM/yyyy')='01/01/1900','',FORMAT(d.ETA,'dd/MM/yyyy')) ETA
,IIF(FORMAT(d.ATA,'dd/MM/yyyy')='01/01/1900','',FORMAT(d.ATA,'dd/MM/yyyy')) ATA
,IIF(FORMAT(d.ATB,'dd/MM/yyyy')='01/01/1900','',FORMAT(d.ATB,'dd/MM/yyyy')) ATB
,IIF(FORMAT(d.ETB,'dd/MM/yyyy')='01/01/1900','',FORMAT(d.ETB,'dd/MM/yyyy')) ETB
,IIF(FORMAT(d.Un_StuffingLocationDate,'dd/MM/yyyy')='01/01/1900','',FORMAT(d.Un_StuffingLocationDate,'dd/MM/yyyy')) Un_StuffingLocationDate
,IIF(FORMAT(d.DateOfClearence,'dd/MM/yyyy')='01/01/1900','',FORMAT(d.DateOfClearence,'dd/MM/yyyy')) DateOfClearence
, v.VehicleNo
,d.Vehicle
,d.Vendor
,d.TransportCost
, case When tt.UpdateStatus IS NULL Then d.ShipmentStatus Else tt.UpdateStatus End as ShipmentStatus 
,d.Remarks
,d.DocumentationId
,com.CompanyName
,com.Address1
,IIF(FORMAT(d.DateOfClearence,'dd/MM/yyyy')='01/01/1900','',FORMAT(d.DateOfClearence,'dd/MM/yyyy')) as DeliveryDate
,et.ExpenseType,'Job Report' as DataColumn4 
,imp.CustomerName as ImporterName
,d.ContainerSize
,d.LCANo
,IIF(FORMAT(d.Dischargedate,'dd/MM/yyyy')='01/01/1900','',FORMAT(d.Dischargedate,'dd/MM/yyyy')) as Dischargedate
,pol.PlaceOfLoadingName
,shy.ShedYardName
,d.FreightCharge
,IIF(FORMAT(d.DocumentHandoverDate,'dd/MM/yyyy')='01/01/1900','',FORMAT(d.DocumentHandoverDate,'dd/MM/yyyy')) DocumentHandoverDate
,IIF(FORMAT(d.CargoHandoverDate,'dd/MM/yyyy')='01/01/1900','',FORMAT(d.CargoHandoverDate,'dd/MM/yyyy')) CargoHandoverDate
,dpt.DepotName
,d.BookingConfirmationReference
,FORMAT(d.ETDDate,'dd/MM/yyyy') as ETDDate
,FORMAT(d.ETACTGPORT,'dd/MM/yyyy') as ETACTGPORT
,FORMAT(d.SHIPARRIVEDDATE,'dd/MM/yyyy') as SHIPARRIVEDDATE
from Sep_Documentation d  
left outer join Sales_Def_Prefix p on d.PrifixId = p.PrifixId  
left outer join Sales_Def_Supplier s on d.SupplierId = s.SupplierId  
left outer join Sales_Customer imp on d.CustomerID_Importer = imp.CustomerID 
left outer join RMG_Prod_Def_UnitType u1 on d.Unit1 = u1.UnitTypID  
left outer join RMG_Prod_Def_UnitType u2 on d.Unit2 = u2.UnitTypID  
left outer join Sales_Customer ctm on d.CustomerID = ctm.CustomerID  
left outer join Sales_Def_PortName pn on d.PortNameId = pn.PortNameId  
left outer join Sales_Def_Vehicle v on d.VehicleID = v.VehicleID  
left outer join CA_Def_Currency c on d.CurrencyId = c.CurrencyId  
left outer join Core_Company com on d.CompanyCode = com.CompanyCode  
Left Join cta tt on d.JobNo=tt.JobNo 
left outer join CF_Def_ExpenseType et on d.ExpenseTypeID=et.ExpenseTypeID 
left outer join CF_DailyJobUpdateEntry du on d.JobNo=du.JobNo  
Left join CF_JobDeliveryInfo di on d.JobNo=di.JobNo   
Left join CF_Def_PlaceOfLoading pol on d.PlaceOfLoadingID=pol.PlaceOfLoadingID
Left join CF_Def_ShedYard Shy on d.ShedYardID=Shy.ShedYardID
Left join CF_Def_DepotName dpt on d.DepotID=dpt.DepotID
where(@DateUse = 'N' OR @DateUse = 'Y'  AND CAST(d.[Date] as Date) between @fromDate and @toDate) 
And(@CustomerID = '' OR d.CustomerID = @CustomerID) 
And(@PortId = '' OR pn.PortId IN (select * from SplitString(@PortId,','))) 
And(@ExpenseTypeID = '' OR d.ExpenseTypeID IN (select * from SplitString(@ExpenseTypeID,','))) 
 And(@JobNo = '' OR d.JobNo IN (select * from SplitString(@JobNo,','))) 
 Order By ctm.CustomerName
 ,et.ExpenseType
GO
