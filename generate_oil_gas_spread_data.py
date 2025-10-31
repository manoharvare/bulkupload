import csv
import random
from datetime import datetime

# === CONFIG ===
output_file = "oil_gas_resource_spread_100k.csv"
num_records = 100000

# === STATIC HEADERS ===
static_headers = [
    "ProjectId", "ActivityId", "ActivityName", "WBS", "WBS Name",
    "Curve", "Calendar", "ResourceId", "Resource Id Name", "Resource Type",
    "Budgeted Units", "Actual Units", "Remaining Units", "Remaining Late Finish"
]

# === WEEKLY HEADERS (as per your sample) ===
weekly_headers = [
    "5-July-25","12-July-25","19-July-25","26-July-25","2-Aug-25","9-Aug-25","16-Aug-25","23-Aug-25","30-Aug-25",
    "6-Sep-25","13-Sep-25","20-Sep-25","27-Sep-25","4-Oct-25","11-Oct-25","18-Oct-25","25-Oct-25","1-Nov-25",
    "8-Nov-25","15-Nov-25","22-Nov-25","29-Nov-25","6-Dec-25","13-Dec-25","20-Dec-25","27-Dec-25","3-Jan-26",
    "10-Jan-26","17-Jan-26","24-Jan-26","31-Jan-26","7-Feb-26","14-Feb-26","21-Feb-26","28-Feb-26","7-Mar-26",
    "14-Mar-26","21-Mar-26","28-Mar-26","4-Apr-26","11-Apr-26","18-Apr-26","25-Apr-26","2-May-26","9-May-26",
    "16-May-26","23-May-26","30-May-26","6-Jun-26","13-Jun-26"
]

all_headers = static_headers + weekly_headers

# === SAMPLE VALUES ===
activity_names = [
    "Excavation", "Concrete", "Steel Work", "Welding", "Painting",
    "Scaffolding", "Electrical", "Instrumentation", "Testing", "Commissioning"
]
resource_roles = [
    ("RES001", "Operator"), ("RES002", "Mason"), ("RES003", "Fitter"), ("RES004", "Welder"),
    ("RES005", "Painter"), ("RES006", "Helper"), ("RES007", "Electrician"),
    ("RES008", "Technician"), ("RES009", "Inspector"), ("RES010", "Supervisor")
]

# === CSV GENERATION ===
with open(output_file, mode='w', newline='') as file:
    writer = csv.writer(file)
    writer.writerow(all_headers)
    
    for i in range(1, num_records + 1):
        project_id = f"PRJ{random.randint(1, 200):03}"
        activity_id = f"ACT{i:05}"
        activity = random.choice(activity_names)
        wbs = f"WBS-{random.randint(1, 500):03}"
        wbs_name = f"{activity} Section"
        curve = "S-Curve"
        calendar = "BaseCal"
        resource_id, resource_name = random.choice(resource_roles)
        resource_type = "Labor"
        
        budgeted = random.randint(500, 5000)
        actual = random.randint(0, budgeted)
        remaining = budgeted - actual
        remaining_late = random.randint(5, 50)

        # Weekly values (decreasing trend)
        weekly_values = [round(max(0.1, random.uniform(0.1, 20.0)), 1) for _ in weekly_headers]

        row = [
            project_id, activity_id, activity, wbs, wbs_name,
            curve, calendar, resource_id, resource_name, resource_type,
            budgeted, actual, remaining, remaining_late
        ] + weekly_values
        
        writer.writerow(row)
    
print(f"✅ Successfully generated {num_records:,} records in '{output_file}'")
