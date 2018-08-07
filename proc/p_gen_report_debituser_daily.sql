CREATE DEFINER=`root`@`%` PROCEDURE `p_gen_report_debituser_daily`(in dDate date)
label_proc:BEGIN
declare iCount int;
declare iDateId int;

declare iReleaseAndExtendCount int;
declare iShouldPayBackCount int;
declare iPaybackUserCount int;
declare iNotPaybackUserCount int;
declare iTodayExtendUserCount int;
declare iAuditingCount int;
declare iOverdueUserCount int;
declare sDate varchar(10);

declare dTodayBeginTime datetime;
declare dTodayEndTime datetime;

set dTodayBeginTime = date_format(dDate, '%Y%m%d');
set dTodayEndTime = date_format(date_add(dDate,interval 1 day), '%Y%m%d');
set iDateId = date_format(dDate, '%Y%m%d');
set sDate = date_format(dDate, '%Y-%m-%d');
#set dDate = date_format(dDate, '%Y-%m-%d');
select count(1) into iCount from IFUserPaybackDailyReport where dateId = iDateId;
if (iCount > 0) then
	delete from  IFUserPaybackDailyReport where dateId = iDateId;
end if;

/*
select ifnull(count(1),0) into iTodayExtendUserCount from IFUserDebitRecord a 
where a.releaseLoanTime < date_add(sDate,interval -7 day) 
	and a.paybackDayTime >= sDate
	and mod(to_days(releaseLoanTime) - to_days(sDate),7 ) = 0
	and a.status in (-2,2,4,1,3,6);
  */
select count(1),
sum(if(status in (2, 6,-2), 1, 0)),sum(if(status = 4, 1, 0)),sum(if(status = 3, 1, 0))
,sum(if(status =1 and date_format(a.paybackDayTime,'%Y-%m-%d') = sDate,1,0))
,sum(if(status =1 and date_format(a.paybackDayTime,'%Y-%m-%d') > sDate,1,0))
into iShouldPayBackCount,iAuditingCount,iOverdueUserCount,iPaybackUserCount,iNotPaybackUserCount,iTodayExtendUserCount
from IFUserDebitRecord a 
where a.releaseLoanTime <= date_add(sDate,interval -6 day) 
	and a.paybackDayTime >= sDate
	and mod(to_days(releaseLoanTime) - to_days(sDate), 7) = 0
	and a.status in (-2,2,4,1,3,6);

#set iShouldPayBackCount = iReleaseAndExtendCount + iTodayExtendUserCount;

if (ifnull(iShouldPayBackCount,0) = 0) THEN
	select iShouldPayBackCount, iPaybackUserCount, iNotPaybackUserCount, 
							iTodayExtendUserCount, iAuditingCount,iOverdueUserCount,dDate;
	leave label_proc;
else 
	set iNotPaybackUserCount = iAuditingCount + iOverdueUserCount + iNotPaybackUserCount;
	insert into IFUserPaybackDailyReport(dateId, shouldPayBackCount, paybackUserCount,notPaybackUserCount, 
							extendUserCount, auditingCount,overdueUserCount, createTime)
	values(iDateId, iShouldPayBackCount, iPaybackUserCount, iNotPaybackUserCount, 
							iTodayExtendUserCount, iAuditingCount,iOverdueUserCount, now());

	select iShouldPayBackCount, iPaybackUserCount, iNotPaybackUserCount, 
							iTodayExtendUserCount, iAuditingCount,iOverdueUserCount;
end if;


END