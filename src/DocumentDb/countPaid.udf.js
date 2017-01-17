function countPaid (registrations) {
   var count = 0; 
   registrations = registrations || [];
   for (var i=0; i<registrations.length; i++) {
       var registration = registrations[i];
        if (registration.paid) {
            count++;
        }
   }

   return count;
}