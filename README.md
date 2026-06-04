**How to RUN**

Option A: Run Both Backend & Frontend Together
 frontend on https://localhost:62523:

`
cd backend
dotnet run --launch-profile http
`

Option B: Run Backend & Frontend Separately (Recommended for Development)
If you prefer running them in separate terminal windows for cleaner logs:

In Terminal 1 (Backend):
`

cd backend
dotnet run --launch-profile http
`
The Scalar API UI is then hosted at http://localhost:5131/scalar/v1.

In Terminal 2 (Frontend):

`
cd frontend
npm start
`



*NOTE:* If on linux start MS SQL using Docker
`docker start news_analyzer_test`
