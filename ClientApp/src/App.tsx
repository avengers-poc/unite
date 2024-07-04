import React from "react";
import * as css from "./AppCss";
import Chat from "./components/chat";
import { Flex } from "@infotrack/zenith-ui";

function App() {
  return (
    <Flex sx={css.appContainer}>
      <Chat />
    </Flex>
  );
}

export default App;
