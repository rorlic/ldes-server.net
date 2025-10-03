/// <reference types="cypress" />

import { Quad_Graph } from 'n3';
import { UrlResponse, rdf, tree } from '../ldes';

export class ViewResponse extends UrlResponse {
  get view(): Quad_Graph {
    var views = this.store.getSubjects(rdf.type, tree.Node, null);
    expect(views.length).to.be.lessThan(2);
    return views.shift();
  }

  get viewName(): string | undefined {
    const view = this.view?.value;
    return !view ? undefined : view.substring(view.lastIndexOf('/') + 1);
  }

  expectDefined(viewName: string): any {
    expect(this.viewName).to.equal(viewName);
  }

}