/// <reference types="cypress" />

import { Quad_Graph } from 'n3';
import { UrlResponse, rdf, tree } from '../ldes';

export class ViewsResponse extends UrlResponse {
  get Views(): Quad_Graph[] {
    return this.store.getGraphs(null, rdf.type, tree.Node);
  }

  private extractViewName(q: Quad_Graph): string {
    return q.value.split('/').reverse().shift();
  }

  get viewNames(): string[] {
    return this.Views.map(x => this.extractViewName(x));
  }

  expectOnlyEventSource(): any {
    const views = this.Views;
    expect(views.length).to.equal(1);

    const viewName = this.viewNames.shift();
    expect(viewName).to.equal('');
  }

  expectAllDefined(viewNames: string[]): any {
    const views = this.Views;
    expect(views.length - 1).to.equal(viewNames.length);
    expect(this.viewNames.filter(x => !!x)).to.eql(viewNames);
  }

}