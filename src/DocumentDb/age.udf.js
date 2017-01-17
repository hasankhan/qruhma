function age (dob) { 
    if (!dob) return null;
   return new Date().getFullYear() - parseInt(dob.substring(0, 4));
}