function hasAttended (registrations, seminarIds) { 
   seminarIds = Array.isArray(seminarIds) ? seminarIds : [seminarIds];
   registrations = registrations || [];
   for (var i=0; i<registrations.length; i++) {
       var registration = registrations[i];
       if (seminarIds.indexOf(registration.seminarId)>-1) {
           return true;
       }
   }

   return false;
}