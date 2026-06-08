## How to Run

### Option A: Run Both Backend & Frontend Together

The frontend will be available at **https://localhost:62523**.

```bash
cd backend
dotnet run --launch-profile http
```

---

### Option B: Run Backend & Frontend Separately (Recommended for Development)

Run the backend and frontend in separate terminal windows for cleaner logs and easier debugging.

#### Terminal 1 – Backend

```bash
cd backend
dotnet run --launch-profile http
```

The Scalar API UI will be available at:

```text
http://localhost:5131/scalar/v1
```

#### Terminal 2 – Frontend

```bash
cd frontend
npm start
```

The frontend will be available at:

```text
https://localhost:62523
```

## Run Tests

Navigate to the frontend directory and run the test suite:

```bash
cd frontend
npm run test
```


## Database Setup
<img width="889" height="286" alt="image" src="https://github.com/user-attachments/assets/b3e6ac9e-e049-45c6-bc82-860c4a1d449b" />

1. Ensure that the SQL Server database is running.

2. Connect to the database using the following JDBC URL:

   ```properties
   jdbc:sqlserver://127.0.0.1:1433;databaseName=VectorDb
   ```

3. Enter your database credentials:
   - **Username:** `<your-username>`
   - **Password:** `<your-password>`

4. Verify that the database connection is established successfully before starting the application.

*NOTE:* If on linux start MS SQL using Docker. You can use DBeaver CE for viewing the DB 

`docker start news_analyzer_test`
