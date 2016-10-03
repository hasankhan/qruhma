function hasRegisteredAll (registrations, seminarIds) { 
   seminarIds = Array.isArray(seminarIds) ? seminarIds : [seminarIds];
   registrations = registrations || [];
   for (var i=0; i<registrations.length; i++) {
       var registration = registrations[i];
       var index = seminarIds.indexOf(registration.seminarId);
       if (index>-1) {
           seminarIds.splice(index, 1);
       }
   }

   return seminarIds.length === 0;
}