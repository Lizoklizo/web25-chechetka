import React from "react";
import 'bootstrap/dist/css/bootstrap.min.css';
import Dashboard from "./components/Dashboard";  // Импортируем компонент Dashboard


const App: React.FC = () => {
  return (
      <div>
          <Dashboard />  {/* Вставляем компонент Dashboard */}
      </div>
  );
};

export default App;