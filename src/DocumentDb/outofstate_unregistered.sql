select s.id, s.firstName, s.lastName, s.phone, s.state from s where  s.state != 'WA' and not is_defined(s.nophone) and not udf.hasAttendedAny(s.registrations, [1274])