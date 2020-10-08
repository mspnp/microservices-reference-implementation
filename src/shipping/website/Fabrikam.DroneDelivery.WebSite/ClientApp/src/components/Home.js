import React, { Component } from 'react';
import { ReactBingmaps } from 'react-bingmaps';

export class Home extends Component {
  static displayName = Home.name;

  render () {
    return (
      <div>
        <h1>Hello, world!</h1>
            <p>Welcome to your new single-page application, built with:</p>

            <div style={{ height: "600px", width: "1000px" }}>
                <ReactBingmaps
                    
                    disableStreetside={true}
                    navigationBarMode={"compact"}
                    bingmapKey="ApNNsibpeT5vu3CzJDsU2qX755x7lF8N-tlrSUGc9iaUthHe0HcMzcX1B2yHYzec"
                    center={[38.955710, -94.430087]}
                >
                </ReactBingmaps>
            </div>
             


      </div>
    );
  }
}
