var App = (function(){
	function App () {
	}

	App.prototype.run = function(seminarId, homePageUrl, rosterUrl, studentsUrl, slackApiToken) {
		var dataTable;
		var volunteersList;
		var studentsRoster;

		var seminarList = $('#seminarList');
		for (var i = seminars.length; i>0; i--) {
			var s = seminars[i - 1];
			seminarList.append('<li><a data-val="' + s.id + '" href="#">' + s.name + '</a></li>');
		}

		seminarList.on('click', 'li a', function () {
			var id = $(this).data('val');
			window.location = homePageUrl + "?id=" + id + window.location.hash;
		});

		$('#importBtn').click(importStudentList);

		$('#queryBtn').click(queryStudents);

		var seminar = _.find(seminars, function (s) { return s.id === seminarId; });
		if (seminar) {
			$('#seminarName').text(seminar.name);
			$('#seminarTitle').text(seminar.title);
			$('#seminarDate').text(seminar.date);
			$('#seminarInstructor').text(seminar.instructor);

			var daysFromNow = moment(seminar.date, 'MMM Do, YYYY').fromNow();
			$('#daysRemaining').text(daysFromNow);
		}

		function queryStudents() {
			var queryText = $('#queryText').val();
			var queryResultText = $('#queryResultText');

			$.getJSON(studentsUrl, {
				n: 1000,
				q: queryText
			}).done(function (data) {
				queryResultText.val(JSON.stringify(data, null, '\t'));
			}).fail(function (err) {
				queryResultText.val('Error: ' + err.responseText);
			})
		}

		function parseDate(strDate) {
			if (!strDate) {
				return null;
			}
			return new Date(strDate);
		}

		function importStudentList() {
			var importBtn = $('#importBtn');
			importBtn.hide();

			var importStatus = $('#importStatus');
			importStatus.text('processing...').show();

			var upsertList = [];			

			// download complete list of students
			$.getJSON(studentsUrl + '?n=10000', function (res) {
				var allStudents = _.reduce(res, function (m, x) { return m.concat(x.Documents); }, []);

				// for each student in the roster, find and update the entry in master list or add a new entry
				studentsRoster.forEach(function (student) {
					var entry = addOrUpdateStudent(allStudents, student);
					if (entry) {
						upsertList.push(entry);
					}
				});

				// post the updated list to the server
				$.ajax({
					type: 'post',
					url: studentsUrl + '/update',
					data: JSON.stringify(upsertList),
					success: function () {
						importStatus.text('done!');
					},
					contentType: 'application/json',
					error: function (xhr) {
						importStatus.text('failed!');
						importBtn.show();
					}
				});
			});
		}

		function parseNum(strNum) {
			if (!strNum) {
				return 0;
			}
			return parseInt(strNum);
		}

		function toTitleCase(text)
		{
			if (!text) {
				return text;
			}

			return text.replace(/\w\S*/g, function (word) {
				return word.charAt(0).toUpperCase() + word.substr(1).toLowerCase();
			});
		}

		function createRegistration(student) {
			return {
				seminarId: seminarId,
				regDate: parseDate(student["Registration Date"]),
				paid: student["Fully Paid"] === "Yes",
				amount: parseNum(student["Amt"]),
				method: student["Method"],
				ref: (student["Reference"] || "").toString(),
				payDate: parseDate(student["Payment Date"])
			};
		}

		function addOrUpdateStudent(allStudents, student) {
			var found = _.find(allStudents, function (s) { return s.id === student["Email"]; });
			if (!found) {
				return {
					id: student["Email"],
					firstName: toTitleCase(student["First Name"]),
					lastName: toTitleCase(student["Last Name"]),
					gender: student["Gender"],
					phone: student["Phone"],
					dob: parseDate(student["DOB"]),
					address: student["Address"],
					city: toTitleCase(student["City"]),
					state: student["State"],
					zip: student["Postal/Zip"],
					country: student["Country"],
					registrations: [createRegistration(student)]
				};
			}
			else {
				var before = JSON.stringify(found);
				found.firstName = found.firstName || toTitleCase(student["First Name"]);
				found.lastName = found.lastName || toTitleCase(student["Last Name"]);
				found.gender = found.gender || student["Gender"];
				found.phone = found.phone || student["Phone"];
				found.dob = found.dob|| parseDate(student["DOB"]);
				found.address = found.address || student["Address"];
				found.city = found.city || toTitleCase(student["City"]);
				found.state = found.state || student["State"];
				found.zip = found.zip || student["Postal/Zip"];
				found.country = found.country || student["Country"];

				var foundReg = _.find(found.registrations, function (r) { return r.seminarId === seminarId; });
				var newReg = createRegistration(student);
				if (!foundReg) {
					found.registrations.push(newReg);
				}
				else {
					// just update the existing registration by overwriting the values
					_.extend(foundReg, newReg);
				}
				var after = JSON.stringify(found);
				if (before !== after) {
					return found;
				}
				else {
					// no change
					return null;
				}
			}
		}

		function persistTabs() {
			// show active tab on reload
			if (location.hash !== '') $('a[href="' + location.hash + '"]').tab('show');

			// remember the hash in the URL without jumping
			$('a[data-toggle="tab"]').on('shown.bs.tab', function (e) {
				if (history.pushState) {
					history.pushState(null, null, '#' + $(e.target).attr('href').substr(1));
				} else {
					location.hash = '#' + $(e.target).attr('href').substr(1);
				}
			});
		}

		function downloadRoster() {
			var oReq = new XMLHttpRequest();
			oReq.open("GET", rosterUrl + '?id=' + seminarId, true);
			oReq.responseType = "arraybuffer";
			oReq.onload = function (e) {
				var arraybuffer = oReq.response;

				/* convert data to binary string */
				var data = new Uint8Array(arraybuffer);
				var arr = new Array();
				for (var i = 0; i !== data.length; ++i) arr[i] = String.fromCharCode(data[i]);
				var bstr = arr.join("");

				/* Call XLSX */
				var workbook = XLSX.read(bstr, { type: "binary" });

				transform(workbook);

				$('importBtn').show();
				$('importStatus').hide();
			};

			oReq.send();
		}

		function calculateAge(birthday) {
			var ageDiffMSec = Date.now() - birthday.getTime();
			var ageDateEpoc = new Date(ageDiffMSec);
			return Math.abs(ageDateEpoc.getUTCFullYear() - 1970);
		}

		function transform(workbook) {
			var first_sheet_name = workbook.SheetNames[0];
			var worksheet = workbook.Sheets[first_sheet_name];
			var range = worksheet['!range'];

			var headers = [];
			for (var col = 0; col < range.e.c; col++) {
				var alphabet = String.fromCharCode(65 + col);
				var title = worksheet[alphabet + 9].v;
				headers.push({ alphabet: alphabet, title: title });
			}

			studentsRoster = [];
			for (var row = 10; row < range.e.r; row++) {

				var student = {};
				headers.forEach(function (h) {
					var cell = worksheet[h.alphabet + row];
					var value = cell ? cell.v : null;
					student[h.title] = value;
				});

				var dob = student['DOB'];
				student['Age'] = dob ? calculateAge(new Date(dob)): null;

				student['RegistrationStamp'] = new Date(student['Registration Date']).toDateString();

				student['Name'] = student['First Name'] + ' ' + student['Last Name'];

				// ignore students without a name
				if (student['First Name'] || student['Last Name']) {
					studentsRoster.push(student);
				}
			}

			renderCharts(studentsRoster);
			renderStats(studentsRoster);

			dataTable = $('#rosterTable').DataTable({
				data: studentsRoster,
				columns: [
					{ title: '', searchable: false, orderable: false, defaultContent: '' },
					{ title: 'First', data: 'First Name' },
					{ title: 'Last', data: 'Last Name' },
					{ title: 'Gender', data: 'Gender' },
					{ title: 'Email', data: 'Email' },
					{ title: 'City', data: 'City' },
					{ title: 'State', data: 'State' },
					{ title: 'Reg. Date', data: 'RegistrationStamp' },
					{ title: 'Paid', data: 'Fully Paid' }
				],
				paging: false
			}).on('order.dt search.dt', function () {
				dataTable.column(0, {search:'applied', order:'applied'})
					.nodes()
					.each(function (cell, i) { 
						cell.innerHTML = i+1; } 
					);
			});
				
			dataTable.draw();

			$('#loading').hide();
			$('#report').show();

			fetchVolunteers();
		}

		function renderStats(studentsRoster) {
			var genderCount = _.countBy(studentsRoster, function (x) { return x['Gender'] + '_' + x['Fully Paid']; });
				
			var paidBrothers = genderCount['m_Yes'] || 0;
			var unpaidBrothers = genderCount['m_No'] || 0;
			$('#brothersCount').text(paidBrothers + '/' + (paidBrothers + unpaidBrothers));
				
			var paidSisters = genderCount['f_Yes'] || 0;
			var unpaidSisters = genderCount['f_No'] || 0;
			$('#sistersCount').text(paidSisters + '/' + (paidSisters + unpaidSisters));
				
			var totalCount = paidBrothers + paidSisters;
			$('#studentCount').text(totalCount + '/' + studentsRoster.length);
								
			var today = new Date();
			today.setHours(0, 0, 0, 0);
			today = today.toDateString();
			var regToday = _.filter(studentsRoster, function (s) { return s['RegistrationStamp'] === today; }).length;
			$('#registeredToday').text(regToday);
		}

		function renderCharts(studentsRoster) {
			var dateCountData =
				_.map(
					_.values(
						_.groupBy(studentsRoster, function (x) { return x['RegistrationStamp']; })
					),
					function(x) {
						return [
							new Date(x[0]['RegistrationStamp']).getTime(),
							x.length
						];
					}
				);

			dateCountData = _.sortBy(dateCountData, function(x) { return x[0]; });

			$('#regByDayChart').highcharts('StockChart', {
				title: {
					text: 'Registrations by day'
				},
				series: [{
					name: 'Count',
					data: dateCountData
				}]
			});

			var cityCountData =
				_.sortBy(
					_.map(
						_.values(
							_.groupBy(studentsRoster, function (x) { return x['City'].toString().toLowerCase().trim().split(' ')[0]; })
						),
						function(x) {
							return [
								x[0]['City'].toString().toLowerCase(),
								x.length
							];
						}
					),
					function(x) { return -x[1]; }
				);

			var brotherAges = calculateAgeDistribution(studentsRoster, 'm');
			var sisterAges = calculateAgeDistribution(studentsRoster, 'f');

			$('#ageDistChart').highcharts({
				chart: {
					type: 'column'
				},
				title: {
					text: 'Student age distribution'
				},
				xAxis: {
					min: 0,
					stackLabels: {
						enabled: true
					},
					title: { text: "Age" }
				},
				yAxis: {
					title: { text: "Count" }
				},
				legend: {
					align: 'right',
					x: -30,
					verticalAlign: 'top',
					y: 25,
					floating: true,
					backgroundColor: 'white',
					borderColor: '#CCC',
					borderWidth: 1,
					shadow: false
				},
				plotOptions: {
					column: {
						stacking: 'normal',
						dataLabels: {
							enabled: true
						}
					}
				},
				series: [{
					name: 'Brother',
					data: brotherAges
				}, {
					name: 'Sister',
					data: sisterAges
				}]
			});

			cityCountData = _.filter(cityCountData, function (x) { return x[1] > 1; });
			var cityCountDataSum = _.reduce(cityCountData, function (m, x) { return m+x[1]; }, 0);
			cityCountData.push(['other', studentsRoster.length - cityCountDataSum]);

			$('#regByCityChart').highcharts({
				chart: {
					type: 'column'
				},
				title: {
					text: 'Registrations by city'
				},
				xAxis: {
					type: 'category',
					labels: {
						rotation: -45,
						style: {
							fontSize: '13px',
							fontFamily: 'Verdana, sans-serif'
						}
					}
				},
				yAxis: {
					min: 0,
					title: {
						text: 'Registration count'
					}
				},
				legend: {
					enabled: false
				},
				series: [{
					name: 'Registration Count',
					data: cityCountData,
					dataLabels: {
						enabled: true,
						rotation: -90,
						color: '#FFFFFF',
						align: 'right',
						y: 10, // 10 pixels down from the top
						style: {
							fontSize: '13px',
							fontFamily: 'Verdana, sans-serif'
						}
					}
				}]
			});
		}

		function calculateAgeDistribution(studentsRoster, gender) {
			var ageDist = _.map(
						_.groupBy(
							_.filter(studentsRoster, function (s) { return s['Gender'] === gender && !isNaN(s['Age']); }),
							function (s) {
								return s['Age'];
							}
						),
						function (x) {
							return [
								x[0]['Age'],
								x.length
							];
						}
					);

			return ageDist;
		}

		function fetchVolunteers() {
			$.getJSON('https://slack.com/api/users.list?token=' + slackApiToken, function (result) {
				volunteersList = result.members;

				volunteersList.forEach(function (v) {
					var name = (v.real_name || v.name).toLowerCase();
					var email = (v.profile.email || '').toLowerCase();

					v.isMatch = function (e, n) {
						return name === (n || '').toLowerCase() || email === (e || '').toLowerCase();
					};
				});

				var indexes = dataTable.rows().eq(0).filter(function (rowIdx) {
					var email = dataTable.cell(rowIdx, 4).data().toLowerCase();
					var name = (dataTable.cell(rowIdx, 1).data() + ' ' + dataTable.cell(rowIdx, 2).data()).toLowerCase();
					return _.any(volunteersList, function (v) {
						return v.isMatch(email, name);
					});
				});
				dataTable.rows (indexes).nodes().to$().addClass('volunteer');

				indexes = dataTable.rows().eq(0).filter(function(rowIdx) { return dataTable.cell(rowIdx, 8).data() === 'No'; });
				dataTable.rows(indexes).nodes().to$().addClass('not_paid');

				var unregistered = _.filter(volunteersList, function (v) {
					// those volunteers are not in the students list
					return v.profile.email && !v.deleted && !v.is_bot && !_.any(studentsRoster, function (s) {
						return v.isMatch(s.Email, s.Name);
					});
				});

				$('#unreg_volunteer_count').text(unregistered.length);

				var volunteerTable = $('#volunteersTable').DataTable({
					data: unregistered,
					columns: [
						{ title: 'Name', data: function (x) { return x.real_name || x.name || ''; } },
						{ title: 'Email', data: 'profile.email' }
					],
					paging: false,
					searching: false
				});
			});
		}

		persistTabs();
		downloadRoster();
	};

	return App;
}());
